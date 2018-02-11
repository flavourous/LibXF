using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Text;
using Xamarin.Forms;

namespace LibXF.Controls
{
    public class BindableStack : StackLayout
    {
        public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create("ItemsSource", typeof(IEnumerable), typeof(BindableStack));
        public IEnumerable ItemsSource { get => GetValue(ItemsSourceProperty) as IEnumerable; set => SetValue(ItemsSourceProperty, value); }

        public static readonly BindableProperty ItemTemplateProperty = BindableProperty.Create("ItemTemplate", typeof(DataTemplate), typeof(BindableStack));
        public DataTemplate ItemTemplate { get => (DataTemplate)GetValue(ItemTemplateProperty); set => SetValue(ItemTemplateProperty, value); }

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();
            RecreateView();
        }
        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);
            if (propertyName == ItemsSourceProperty.PropertyName || propertyName == ItemTemplateProperty.PropertyName)
                RecreateView();
        }
        INotifyCollectionChanged last;
        void RecreateView()
        {
            Children.Clear();
            if(last!=null) last.CollectionChanged -= Source_CollectionChanged;
            if (ItemsSource == null || ItemTemplate == null) return;
            if (ItemsSource is INotifyCollectionChanged nc) nc.CollectionChanged += Source_CollectionChanged; 
            foreach(var c in ItemsSource)
            {
                var v = (View)ItemTemplate.CreateContent();
                v.BindingContext = c;
                Children.Add(v);
            }
        }

        private void Source_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RecreateView();
        }
    }
}