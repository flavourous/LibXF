using System;
using System.Runtime.CompilerServices;
using Xamarin.Forms;

namespace LibXF.Controls
{
    public class Flip90 : ContentView
    {
        public static readonly BindableProperty FlippedContentProperty = BindableProperty.Create("FlippedContent", typeof(View), typeof(Flip90));
        public View FlippedContent { get => (View)GetValue(FlippedContentProperty); set => SetValue(FlippedContentProperty, value); }

        SizeRequest? sr;
        void SetContent(View sFlip)
        {
            var nsr = sFlip.Measure(double.PositiveInfinity, double.PositiveInfinity);
            if (sr == null || nsr.Request.Width != sr.Value.Request.Width || nsr.Request.Height != sr.Value.Request.Height)
            {
                sr = nsr;
                Content = new ContentView
                {
                    Content = sFlip,
                    Rotation = -90
                };
                Content.WidthRequest = sr.Value.Request.Width;
                Content.HeightRequest = sr.Value.Request.Height;
            }
        }

        View ofc;
        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);
            if (propertyName == FlippedContentProperty.PropertyName)
            {
                if (ofc != null) ofc.MeasureInvalidated -= FlippedContent_MeasureInvalidated;
                if (FlippedContent != null)
                {
                    FlippedContent.MeasureInvalidated += FlippedContent_MeasureInvalidated;
                    SetContent(FlippedContent);
                }
                else Content = null;
            }
        }

        private void FlippedContent_MeasureInvalidated(object sender, EventArgs e)
        {
            SetContent(sender as View);
        }
    }
}
