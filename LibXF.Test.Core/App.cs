using LibXF.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;

namespace LibXF.Test.Core
{
    
    public class App : Application
    {
        public static (IEnumerable items, IEnumerable rh, IEnumerable ch) Generate(int rows, int cols, int rgrp, int cgrp)
        {
            // make it fit
            var rrows = rows + rows % rgrp;
            var rcols = cols + cols % cgrp;

            // col headres
            var ch = new[]
            {
                Enumerable.Range(0,rcols).Select(c=> (c%cgrp==0)?new CC("Group " + c/cgrp,1,cgrp){ wgrp = c/cgrp }:null).ToArray(),
                Enumerable.Range(0,rcols).Select(c=> new CC("Item " + c,1,1){ wgrp = c/cgrp }).ToArray(),
            }.Skip(cgrp == 1 ? 1 : 0).ToArray();
            //row headers
            var rh = Enumerable.Range(0, rrows)
                      .Select(r => new[]
                        {
                            (r%rgrp==0)?new CC("Group " + r/rgrp,rgrp,1){ wgrp = r/rgrp }:null,
                            new CC("Item " + r,1,1){ wgrp = r/rgrp }
                        }.Skip(rgrp==1 ? 1 : 0))
                        .ToArray();
            // data!
            var dat = Enumerable.Range(0, rrows).Select(r => Enumerable.Range(0, rcols).Select(c => new CC( c + "," + r, 1, 1)).ToArray()).ToArray();

            return (dat, rh, ch);
        }

        class CC
        {
            public CC(String val, int r, int c)
            {
                this.val = val;
                this.c = c;
                this.r = r;
            }
            public static implicit operator CC (String s)
            {
                return new CC(s, 1, 1);
            }
            public String val { get; set; }
            public int r { get; set; }
            public int c { get; set; }
            public int rgrp { get; set; }
            public int wgrp { get; set; }
        }
        Dictionary<String, Dictionary<String, Func<View>>> Cases = new Dictionary<string, Dictionary<string, Func<View>>>
        {
            { "BindableGrid", GridCases },
            { "BindableStack", StackCases },
            { "Text Stuff",TextStuff }
        };

        static Dictionary<String, Func<View>> TextStuff = new Dictionary<string, Func<View>>
        {
            {
                "Label90",
                () => {
                    var g = new Grid();
                    for(int i = 0; i<5; i++)
                    {
                        g.RowDefinitions.Add(new RowDefinition{Height = GridLength.Auto });
                        for(int j=0;j<5;j++)
                        {
                            if(i==0) g.ColumnDefinitions.Add(new ColumnDefinition{Width= GridLength.Auto });
                            var f9 = new Flip90 { FlippedContent = new Label{ BackgroundColor= Color.Red, Text = " flip " +j } };
                            var nn = new Label { BackgroundColor= Color.Tomato,Text= j > 2 ? "more " + j : "." };
                            View use = i == 0? f9:(View)nn;
                            use.HorizontalOptions = LayoutOptions.Center;
                            Grid.SetRow(use,i);
                            Grid.SetColumn(use,j);
                            g.Children.Add(use);
                        }
                    }
                    return g;
                }
            }
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
                    ItemsSource = new object[]
                    {
                        new object[] { "This", "ia", "row" },
                        new object[] { "1This", "4ia", "r567ow" },
                        new object[] { "32This", "55ia", "ro??w" },
                    },
                    ItemTemplate = new DataTemplate(() =>
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
            },
            {
                "MAshed Grid" ,
                () => new BindableGrid
                {
                    ItemsSource = new CC[][]
                    {
                        new CC[] { "This", "ia", "row" },
                        new CC[] { new CC("MASHED",2,2), "LOLDONTCARE", "r567ow" },
                        new CC[] { "LOLDONTCARE", "LOLDONTCARE", "ro??w" },
                    },
                    ItemTemplate = new DataTemplate(() =>
                    {
                        return new Grid
                        {
                            Children =
                            {
                                new BoxView() { BackgroundColor = Color.Aquamarine, Margin = new Thickness(-1) },
                                new Label().Bind(Label.TextProperty, "Data.val")
                            }
                        };
                    }),
                    CellInfo = new CellInfoBinder()
                                .Bind(CellInfoBinder.RowSpanProperty, "r")
                                .Bind(CellInfoBinder.ColumnSpanProperty, "c")
                }
            },
            {
                "Headered Grid" ,
                () => new BindableGrid
                {
                    FrozenHeaders = true,
                    ItemsSource = new []
                    {
                        new [] { "This\n is\n fat\n ok", "ia looooooooooooooooooooooooooong", "row" },
                        new [] { "1This", "4ia", "r567ow" },
                        new [] { "32This", "55ia", "ro??w" },
                    },
                    ColumnHeaders = new[]{ new[] { "c1", "c2", "c3 mo mo mo" } },
                    RowHeaders = new[]{ new[] { "r1" }, new[] { "r2\n hai\n hao" }, new[]{ "r3" } },
                    RowHeadersTemplate = new DataTemplate(() =>
                    {
                        return new Grid
                        {
                            Children =
                            {
                                new Label().Bind(Label.TextProperty, "Data")
                            }
                        };
                    }),
                    ColumnHeadersTemplate = new DataTemplate(() =>
                    {
                        return new Grid
                        {
                            Children =
                            {
                                new Label().Bind(Label.TextProperty, "Data")
                            }
                        };
                    }),
                    ItemTemplate = new DataTemplate(() =>
                    {
                        return new Grid
                        {
                            Children =
                            {
                                new Label().Bind(Label.TextProperty, "Data")
                            }
                        };
                    })
                }
            },
            {
                "Mashed Headered Grid" ,
                () => new BindableGrid
                {
                    ItemsSource = new CC[][]
                    {
                        new CC[] { "This", "ia", "row" },
                        new CC[] { "1This", new CC("mash",1,2), "no" },
                        new CC[] { "32This", "55ia", "ro??w" },
                    },
                    ColumnHeaders = new CC[][]
                    {
                        new CC[] { new CC("hmash",1,2), "nope", "hai" },
                        new CC[] { "c1", "c2", "c3" }
                    },
                    RowHeaders = new CC[][]
                    {
                        new CC[] { "r1",">" },
                        new CC[] { new CC("r23",2,1),">" },
                        new CC[] { "nope",">" } },
                    RowHeadersTemplate = new DataTemplate(() =>
                    {
                        return new Grid
                        {
                            Children =
                            {
                                new BoxView() { BackgroundColor = Color.Gold, Margin = new Thickness(-1) },
                                new Label().Bind(Label.TextProperty, "Data.val")
                            }
                        };
                    }),
                    ColumnHeadersTemplate = new DataTemplate(() =>
                    {
                        return new Grid
                        {
                            Children =
                            {
                                new BoxView() { BackgroundColor = Color.Cyan, Margin = new Thickness(-1) },
                                new Label().Bind(Label.TextProperty, "Data.val")
                            }
                        };
                    }),
                    ItemTemplate = new DataTemplate(() =>
                    {
                        return new Grid
                        {
                            Children =
                            {
                                new BoxView() { BackgroundColor = Color.Aquamarine, Margin = new Thickness(-1) },
                                new Label().Bind(Label.TextProperty, "Data.val")
                            }
                        };
                    }),
                    CellInfo = new CellInfoBinder()
                                .Bind(CellInfoBinder.RowSpanProperty, "r")
                                .Bind(CellInfoBinder.ColumnSpanProperty, "c")
                }
            },
            {
                "Multi Headered Grid" ,
                () => new BindableGrid
                {
                    ItemsSource = new []
                    {
                        new [] { "This", "ia", "row" },
                        new [] { "1This", "nah", "no" },
                        new [] { "32This", "55ia", "ro??w" },
                    },
                    ColumnHeaders = new []
                    {
                        new [] { "hhai", "nope", "hai" },
                        new [] { "c1", "c2", "c3" }
                    },
                    RowHeaders = new []
                    {
                        new [] { "r1",">" },
                        new [] { "r2",">" },
                        new [] { "r3",">" }
                    },
                    RowHeadersTemplate = new DataTemplate(() =>
                    {
                        return new Grid
                        {
                            Children =
                            {
                                new BoxView() { BackgroundColor = Color.Gold, Margin = new Thickness(-1) },
                                new Label().Bind(Label.TextProperty, "Data")
                            }
                        };
                    }),
                    ColumnHeadersTemplate = new DataTemplate(() =>
                    {
                        return new Grid
                        {
                            Children =
                            {
                                new BoxView() { BackgroundColor = Color.Cyan, Margin = new Thickness(-1) },
                                new Label().Bind(Label.TextProperty, "Data")
                            }
                        };
                    }),
                    ItemTemplate = new DataTemplate(() =>
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
            },
            {
                "Big MH Grid" ,
                () =>
                {
                    var data = Generate(10,40,2,5);
                    return new BindableGrid
                    {
                        ItemsSource = data.items,
                        ColumnHeaders = data.ch,
                        RowHeaders = data.rh,
                        RowHeadersTemplate = new DataTemplate(() =>
                        {
                            return new Grid
                            {
                                Children =
                                {
                                    new BoxView() { BackgroundColor = Color.Gold, Margin = new Thickness(-1) },
                                    new Label{ VerticalTextAlignment= TextAlignment.Center, HorizontalTextAlignment = TextAlignment.End }.Bind(Label.TextProperty, "Data.val")
                                }
                            };
                        }),
                        ColumnHeadersTemplate = new DataTemplate(() =>
                        {
                            return new Grid
                            {
                                Children =
                                {
                                    new BoxView() { BackgroundColor = Color.Cyan, Margin = new Thickness(-1) },
                                    new Label{ VerticalTextAlignment = TextAlignment.End, HorizontalTextAlignment = TextAlignment.Center }.Bind(Label.TextProperty, "Data.val")
                                }
                            };
                        }),
                        ItemTemplate = new DataTemplate(() =>
                        {
                            return new Grid
                            {
                                Children =
                                {
                                    new BoxView() { BackgroundColor = Color.Aquamarine, Margin = new Thickness(-1) },
                                    new Label{ VerticalTextAlignment = TextAlignment.Center, HorizontalTextAlignment = TextAlignment.Center }.Bind(Label.TextProperty, "Data.val")
                                }
                            };
                        }),
                        CellInfo = new CellInfoBinder()
                                    .Bind(CellInfoBinder.RowSpanProperty, "r")
                                    .Bind(CellInfoBinder.ColumnSpanProperty, "c")
                    };
                }
            },
            {
                "Big MH Frozen Grid" ,
                () =>
                {
                    var data = Generate(25,23,1,1);
                    return new BindableGrid
                    {
                        FrozenHeaders=true,
                        ColumnHeaders = data.ch,
                        RowHeaders = data.rh,
                        RowHeadersTemplate = new DataTemplate(() =>
                            new Label{ VerticalTextAlignment= TextAlignment.Center, HorizontalTextAlignment = TextAlignment.End }.Bind(Label.TextProperty, "Data.val")
                        ),
                        ColumnHeadersTemplate = new DataTemplate(() =>
                            new Label{ VerticalTextAlignment = TextAlignment.End, HorizontalTextAlignment = TextAlignment.Center }.Bind(Label.TextProperty, "Data.val")
                        ),
                        ItemTemplate = new DataTemplate(() =>
                            new Grid
                            {
                                WidthRequest = 50.0,
                                HeightRequest = 50.0,
                                Children=
                                {
                                    new Label{ VerticalTextAlignment = TextAlignment.Center, HorizontalTextAlignment = TextAlignment.Center, BackgroundColor = Color.White, Margin = new Thickness(1,1,0,0) }.Bind(Label.TextProperty, "Data.val")
                                }
                            }
                        ),
                        CellInfo = new CellInfoBinder()
                                    .Bind(CellInfoBinder.RowSpanProperty, "r")
                                    .Bind(CellInfoBinder.ColumnSpanProperty, "c"),
                        ItemsSource = data.items,
                    };
                }
            },
            {  "Frozen headless stack" ,
                () =>
                {
                    var data = Generate(25,1,1,1);
                    return new BindableGrid
                    {
                        FrozenHeaders=true,
                        RowHeaders = data.rh,
                        RowHeadersTemplate = new DataTemplate(() =>
                            new Label{ VerticalTextAlignment= TextAlignment.Center, HorizontalTextAlignment = TextAlignment.End }.Bind(Label.TextProperty, "Data.val")
                        ),
                        ItemTemplate = new DataTemplate(() =>
                            new Grid
                            {
                                WidthRequest = 50.0,
                                HeightRequest = 50.0,
                                Children=
                                {
                                    new Label{ VerticalTextAlignment = TextAlignment.Center, HorizontalTextAlignment = TextAlignment.Center, BackgroundColor = Color.White, Margin = new Thickness(1,1,0,0) }.Bind(Label.TextProperty, "Data.val")
                                }
                            }
                        ),
                        ItemsSource = data.items,
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
