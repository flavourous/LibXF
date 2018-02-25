using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace LibXF.Test.Core.Util
{
    public class T1 { public String name => "T1"; }
    public class T2 { public String name => "T2"; }
    public class V1 : ViewCell { public V1() { View = new Label { Text = "View 1" }; } }

    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class TTSXaml : ContentView
	{
		public TTSXaml ()
		{
            InitializeComponent ();
		}

        public List<object> Items => new List<object> { new object(), new T1(), new T2() };
    }
}