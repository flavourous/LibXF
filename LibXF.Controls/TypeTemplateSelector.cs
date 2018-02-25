using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace LibXF.Controls
{
    [ContentProperty("Template")]
    public class TypeTemplate
    {
        public Type DataType { get; set; }
        public Type ViewType { set => Template = new DataTemplate(value); }
        public DataTemplate Template { get; set; }
    }

    [ContentProperty("Mappings")]
    public class TypeTemplateSelector : DataTemplateSelector
    {
        private IList<TypeTemplate> mappings = new List<TypeTemplate>();
        public IList<TypeTemplate> Mappings { get => mappings; set => mappings = value; }
        public DataTemplate Default { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            foreach (var o in Mappings)
            {
                var me = o as TypeTemplate;
                if (item != null && me.DataType == item.GetType())
                    return me.Template;
            }
            return Default;
        }
    }
}
