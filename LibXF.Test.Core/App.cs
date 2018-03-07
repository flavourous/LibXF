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

namespace LibXF.Test.Core
{    
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
        static Dictionary<String, Func<View>> GridCases = new Dictionary<string, Func<View>>
        {
            {
                "Basic Grid" ,
                () => new BindableGrid
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
                            Children =
                            {
                                new BoxView() { BackgroundColor = Color.Aquamarine, Margin = new Thickness(-1) },
                                new Label().Bind(Label.TextProperty, "Data")
                            }
                        };
                    })
                }
            }
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
