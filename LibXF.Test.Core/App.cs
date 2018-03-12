using LibXF.Controls;
using LibXF.Test.Core.Util;
using LibXF.Controls.BindableLayout;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Xamarin.Forms;
using LibXF.Controls.BindableGrid;
using System.Globalization;

namespace LibXF.Test.Core
{    
    public static class AppExtensions
    {
        public static T Bind<T>(this T o, BindableProperty p, Binding b)
            where T : BindableObject
        {
            o.SetBinding(p, b);
            return o;
        }
        public static T Bind<T>(this T o, BindableProperty p, string prop)
            where T : BindableObject
        {
            o.SetBinding(p, prop);
            return o;
        }
    }
    public class App : Application
    {
        
        Dictionary<String, Dictionary<String, Func<View>>> Cases = new Dictionary<string, Dictionary<string, Func<View>>>
        {
            { "BindableGrid", GridCases },
            { "BindableStack", StackCases },
            { "Text Stuff",TextStuff },
            { "Utils",Utils }
        };


        static Dictionary<String, Func<View>> Utils = new Dictionary<string, Func<View>>
        {
            {
                "TypeTemplateSelector", () =>
                {
                    return new ListView
                    {
                        ItemTemplate = new TypeTemplateSelector
                        {
                            Mappings =
                            {
                                new TypeTemplate{ DataType = typeof(T1), ViewType = typeof(V1) },
                                new TypeTemplate{ DataType = typeof(T2), Template = new DataTemplate(() =>new ViewCell{ View= new Label{Text = "T2 templated" } }) }
                            },
                            Default = new DataTemplate(() => new ViewCell{View =  new Label{ Text="Default" } })
                        },
                        ItemsSource = new object[] {new T1(), new T2(), new object()}
                    };
                }
            },
            {
                "TypeTemplateSelectorXaml", () => new Util.TTSXaml()
            }
        };

        static Dictionary<String, Func<View>> TextStuff = new Dictionary<string, Func<View>>
        {
            
            {
                "taplabel", ()=>
                {
                    var tl = new TapLabel
                    {
                        Text = "Jai2\nlabel size\n bigger",
                    };

                    tl.Command = new Command(()=>tl.BackgroundColor =tl.BackgroundColor == Color.DarkBlue ? Color.Crimson : Color.DarkBlue);
                    return tl;
                }
            },
        };
        
        static Dictionary<String, Func<View>> StackCases = new Dictionary<string, Func<View>>
        {
            {
                "Basic Stak",
                () => new BindableStack
                {
                    Orientation = StackOrientation.Horizontal,
                    ItemsSource = new[]{ "One","two","another one" },
                    ItemTemplate = new DataTemplate(() => 
                    {
                        return new Label().Bind(Label.TextProperty, ".");
                    })
                }
            }
        };

        class cci : ICellInfoManager
        {
            readonly double cs;
            public cci(double cs) => this.cs = cs;
            public double GetColumnmWidth(int col, MeasureType mt) => cs;
            public double GetRowHeight(int row, MeasureType mt) => cs;
            public int GetColumnSpan(object cellData) => 1;
            public int GetRowSpan(object cellData) => 1;
        }

        class lgd
        {
            class lc : IValueConverter
            {
                public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
                {
                    if (value is lgd lv)
                        return targetType == typeof(Color) ? Color.FromRgb(lv.x, 0, lv.y) as object : lv.x + "," + lv.y;
                    return targetType == typeof(Color) ? Color.Black as object : "";
                }
                public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
            }
            class rlc : IValueConverter
            {
                Random r = new Random();
                public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
                {
                    return Color.FromRgb(r.Next(255), r.Next(255), r.Next(255));
                }
                public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
            }
            public static IValueConverter converter = new lc();
            public static IValueConverter rconverter = new rlc();

            public int x { get; set; }
            public int y { get; set; }
        }

        static Dictionary<String, Func<View>> GridCases = new Dictionary<string, Func<View>>
        {
            {
                "Basic Grid" , () =>
                {
                    var ci = new cci(30);
                    return new BindableGrid
                    {
                        CellsSource = new List<IList>
                        {
                            new List<object> { "This", "ia", "row" },
                            new List<object> { "1This", "4ia", "r567ow" },
                            new List<object>{ "32This", "55ia", "ro??w" },
                        },
                        CellTemplate= new DataTemplate(() =>
                        {
                            return new Grid
                            {
                                Margin = new Thickness(5),
                                Children =
                                {
                                    new BoxView() { BackgroundColor = Color.Aquamarine, Margin = new Thickness(-1) },
                                    new Label().Bind(Label.TextProperty, "Data")
                                }
                            };
                        }),
                        CellInfo = ci
                    };
                }
            },
            {
                "Large Grid" , () =>
                {
                    var ci = new cci(30);
                    return new BindableGrid
                    {
                        CellsSource = Enumerable.Range(0,255).Select(x=>Enumerable.Range(0,255).Select(y=> new lgd{ x=x,y=y } ).ToList() as IList).ToList(),
                        CellTemplate= new DataTemplate(() =>
                        {
                            return new BoxView
                            {
                                Margin = new Thickness(-1),
                                HorizontalOptions =LayoutOptions.FillAndExpand,
                                VerticalOptions=LayoutOptions.FillAndExpand
                            }
                            .Bind(BoxView.BackgroundColorProperty,new Binding(".", BindingMode.OneWay, lgd.converter));
                        }),
                        CellInfo = ci
                    };
                }
            },
            {
                "Smalliosh With Headers" , () =>
                {
                    var ci = new cci(30);
                    return new BindableGrid
                    {
                        CellsSource = Enumerable.Range(128,172).Select(x=>Enumerable.Range(128,172).Select(y=> new lgd{ x=x,y=y } ).ToList() as IList).ToList(),
                        ColumnHeadersSource = Enumerable.Range(0,3).Select(x=>Enumerable.Range(128,172).Select(y=> new lgd{ x=255-x,y=y } ).ToList() as IList).ToList(),
                        RowHeadersSource = Enumerable.Range(128,172).Select(x=>Enumerable.Range(0,2).Select(y=> new lgd{ x=x,y=255-y } ).ToList() as IList).ToList(),
                        CellTemplate= new DataTemplate(() =>
                        {
                            return new Grid
                            {
                                WidthRequest = 30,
                                HeightRequest = 30,
                                Children =
                                {
                                    new BoxView
                                    {
                                        Margin = new Thickness(1),
                                        HorizontalOptions =LayoutOptions.FillAndExpand,
                                        VerticalOptions=LayoutOptions.FillAndExpand
                                    }
                                    .Bind(BoxView.BackgroundColorProperty,new Binding(".", BindingMode.OneWay, lgd.converter))
                                }
                            };
                        }),
                        CellInfo = ci
                    };
                }
            },
            
        };
        public App()
        {

            MainPage = new NavigationPage
            (
                new ContentPage
                {
                    Title = "LibXF View Tests",
                    Content = new ListView
                    {
                        ItemsSource = Cases.Keys,
                        ItemTemplate = new DataTemplate(() =>
                        {
                            var ret = new Button();
                            ret.Command = new Command(async () =>
                            {
                                var key = ret.BindingContext as String;
                                var page = new ContentPage
                                {
                                    Title = key,
                                    Content = new ListView
                                    {
                                        ItemsSource = Cases[key].Keys,
                                        ItemTemplate = new DataTemplate(() =>
                                        {
                                            var iret = new Button();
                                            iret.Command = new Command(async () =>
                                            {
                                                var ikey = iret.BindingContext as String;
                                                var ipage = new ContentPage
                                                {
                                                    Title = ikey,
                                                    Content = Cases[key][ikey]()
                                                };
                                                await (MainPage as NavigationPage).Navigation.PushAsync(ipage);
                                            });
                                            iret.SetBinding(Button.TextProperty, ".");
                                            return new ViewCell { View = iret };
                                        })
                                    }
                                };
                                await (MainPage as NavigationPage).Navigation.PushAsync(page);
                            });
                            ret.SetBinding(Button.TextProperty, ".");
                            return new ViewCell { View = ret };
                        })
                    }
                }
            );
        }
    }
}
