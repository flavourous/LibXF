using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace LibXF.Controls.BindableLayout
{

    internal static class Utils
    {
        public static List<List<object>> Expand(this IEnumerable dual)
        {
            return Expand(dual, x => x);
        }
        public static List<List<T>> Expand<T>(this IEnumerable dual, Func<object,T> selector)
        {
            return Expand(dual, (x, r, c) => selector(x));
        }
        public static List<List<T>> Expand<T>(this IEnumerable dual, Func<object,int,int,T> selector)
        {
            var ret = new List<List<T>>();
            if (dual != null)
            {
                int ir = 0;
                foreach (var r in dual)
                {
                    int ic = 0;
                    var rd = new List<T>();
                    foreach (var c in r as IEnumerable)
                    {
                        rd.Add(selector(c,ir,ic));
                        ic++;
                    }
                    ret.Add(rd);
                    ir++;
                }
            }
            return ret;
        }
    }

    public interface ITimedDispatcher : IDisposable
    {
        Task<Task> Add(Action t);
        Task<Task<T>> Add<T>(Func<T> t);
    }

    public class TimedDispatcher : ITimedDispatcher
    {
        readonly SemaphoreSlim sadd = new SemaphoreSlim(1, 1), sdisp = new SemaphoreSlim(0, 1);
        readonly Queue<Action> toDispatch = new Queue<Action>();
        readonly Action<Action> AsyncTarget; // i.e. the ui thread
        readonly Task watcher;
        readonly int watchQueueDelay;
        readonly int maxTimeExecuting;
        readonly double restTimeExecutingFactor;
        readonly double restTimeInvokingFactor;
        readonly int restTimeConstant;
        readonly Stopwatch executingTime = new Stopwatch(), invokingTime = new Stopwatch();

        public TimedDispatcher(Action<Action> AsyncTarget, int watchQueueDelay = 500, int maxTimeExecuting = 200, double restTimeInvokingFactor = 0.5, double restTimeExecutingFactor = 0.5, int restTimeConstant = 0)
        {
            this.watchQueueDelay = watchQueueDelay;
            this.maxTimeExecuting = maxTimeExecuting;
            this.restTimeConstant = restTimeConstant;
            this.restTimeExecutingFactor = restTimeExecutingFactor;
            this.restTimeInvokingFactor = restTimeInvokingFactor;
            this.AsyncTarget = AsyncTarget;
            watcher = Task.Run(CheckQueue);
        }

        void Log(String s, params object[] args)
        {
            return;
            var fs = String.Format(s, args);
            Debug.WriteLine("DISPATCHER:{0}: {1}", DateTime.Now, fs);
        }


        // since we are async we can await and yield to not block the taskpool worker
        async Task CheckQueue()
        {
            bool any = false;
            while (!disposed)
            {
                // Check for new things to invoke every 500ms. similar to threadpool.
                if (!any) await Task.Delay(watchQueueDelay);

                // Anything to dispatch? (interlocked with add methods)
                await sadd.WaitAsync();
                any = toDispatch.Count > 0;
                sadd.Release();

                if (any) // only invoke if there is work to do.
                {
                    Log("Starting sweep");
                    int st = toDispatch.Count();
                    invokingTime.Restart();
                    AsyncTarget(() =>
                    {
                        invokingTime.Stop();
                        try
                        {
                            executingTime.Restart();
                            while (executingTime.ElapsedMilliseconds < maxTimeExecuting)
                            {
                                // Take from the queue or stop
                                sadd.Wait();
                                Action torun = toDispatch.Any() ? toDispatch.Dequeue() : null;
                                sadd.Release();

                                // run it or done it.
                                if (torun == null) break;
                                else torun();
                            }
                        }
                        finally
                        {
                            executingTime.Stop();
                            sdisp.Release();
                        }
                    });

                    // wait for the asynctarget to release
                    await sdisp.WaitAsync();

                    // need to delay for this long to give target reprieve
                    int sleepTime = (int)(invokingTime.ElapsedMilliseconds * restTimeInvokingFactor + executingTime.ElapsedMilliseconds * restTimeExecutingFactor + restTimeConstant);

                    Log("dispatched {0}/{1} in a pass taking {2}ms ({4} ticks), so sleeping for {3}ms", toDispatch.Count - st, st, executingTime.ElapsedMilliseconds, sleepTime, executingTime.ElapsedTicks);

                    // now give the dispatching target a rest according to the used time and prio settings
                    await Task.Delay(sleepTime);
                }
            }
        }
        public async Task<Task<T>> Add<T>(Func<T> t)
        {
            var tsk = new TaskCompletionSource<T>();
            await sadd.WaitAsync();
            toDispatch.Enqueue(() =>
            {
                try { tsk.SetResult(t()); }
                catch (Exception e) { tsk.SetException(e); throw; }
            });
            sadd.Release();
            return tsk.Task;
        }
        public async Task<Task> Add(Action t)
        {
            return await Add(() => { t(); return 0; });
        }

        bool disposed = false;
        public void Dispose() => disposed = true;
    }
}
