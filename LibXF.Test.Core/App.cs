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
                Enumerable.Range(0,rcols).Select(c=> (c%cgrp==0)?new CC("Group " + c/cgrp,1,cgrp,-1,-1){ wgrp = c/cgrp }:null).ToArray(),
                Enumerable.Range(0,rcols).Select(c=> new CC("Item " + c,1,1,-1,-1){ wgrp = c/cgrp }).ToArray(),
            }.Skip(cgrp == 1 ? 1 : 0).ToArray();
            //row headers
            var rh = Enumerable.Range(0, rrows)
                      .Select(r => new[]
                        {
                            (r%rgrp==0)?new CC("Group " + r/rgrp,rgrp,1,120,40){ wgrp = r/rgrp }:null,
                            new CC("Item " + r,1,1,120,40){ wgrp = r/rgrp }
                        }.Skip(rgrp==1 ? 1 : 0))
                        .ToArray();
            // data!
            var dat = Enumerable.Range(0, rrows).Select(r => Enumerable.Range(0, rcols).Select(c => new CC( c + "," + r, 1, 1, -1,30)).ToArray()).ToArray();

            return (dat, rh, ch);
        }

        class CC
        {
            public CC(String val, int r, int c, double w, double h)
            {
                this.val = val;
                this.c = c;
                this.r = r;
                this.w = w;
                this.h = h;
            }
            public static implicit operator CC (String s)
            {
                return new CC(s, 1, 1, -1, -1);
            }
            public double w { get; set; }
            public double h { get; set; }
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
            {
                "rortate platifgboin", () =>
                {
                    var vm = new np();
                    Slider sX = new Slider(0,1.0,0.5);
                    Slider sY = new Slider(0,1.0,0.5);
                    Slider sR = new Slider(-180,180,0);
                    Entry sE = new Entry();
                    sX.SetBinding(Slider.ValueProperty, new Binding("AX"){Source= vm });
                    sY.SetBinding(Slider.ValueProperty, new Binding("AY"){Source= vm });
                    sR.SetBinding(Slider.ValueProperty, new Binding("R"){ Source=vm });
                    sE.SetBinding(Entry.TextProperty, new Binding("T"){ Source=vm });


                    Func<Label> xlab = ()=> new Label(){ Text = "X", HorizontalOptions = LayoutOptions.Center };

                    var g = new Grid();
                    for(int i=0;i<3; i++)
                    {
                        g.RowDefinitions.Add(new RowDefinition{ Height=GridLength.Auto});
                        for(int j=0;j<3; j++)
                        {
                            g.ColumnDefinitions.Add(new ColumnDefinition { Width=GridLength.Auto});
                            var use =  xlab();
                            if(i==0)
                            {
                                use.SetBinding(Label.AnchorXProperty, new Binding("AX"){Source=vm});
                                use.SetBinding(Label.AnchorYProperty, new Binding("AY"){Source=vm});
                                use.SetBinding(Label.RotationProperty, new Binding("R"){Source=vm});
                                if(j==1)use.SetBinding(Label.TextProperty, new Binding("T"){Source=vm});
                                use.WidthRequest = 120;
                                use.HeightRequest = 120;
                                use.Margin = new Thickness(-50,0,-50,0);
                                use.VerticalTextAlignment = TextAlignment.Center;
                                use.HorizontalTextAlignment = TextAlignment.Start;
                            }
                            Grid.SetRow(use,i);Grid.SetColumn(use,j);
                            g.Children.Add(use);
                        }
                    }

                    return new StackLayout
                    {
                        HorizontalOptions = LayoutOptions.Center,
                        Children =
                        {
                            g,
                            sX,
                            sY,
                            sR,
                            sE
                        }
                    };
                }

            }
        };
        class np : INotifyPropertyChanged
        {
            public np()
            {
                T = "testing data";
                AX = AY = 0.5;
                R = -90;
            }
            public String T { get => GetProp<String>(); set => SetProp(value); }
            public double R { get => GetProp<double>(); set => SetProp(value); }
            public double AX { get => GetProp<double>(); set => SetProp(value); } 
            public double AY { get => GetProp<double>(); set => SetProp(value); }
            Dictionary<String, object> repo = new Dictionary<string, object>();
            public event PropertyChangedEventHandler PropertyChanged = delegate { };
            public T GetProp<T>([CallerMemberName]String prop=null)
            {
                return repo.ContainsKey(prop) ? (T)repo[prop] : default(T);
            }
            public void SetProp<T>(T val, [CallerMemberName]String prop = null)
            {
                repo[prop] = val;
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
            }
        }

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
                "Basic Grid Rotated TExt" ,
                () => new BindableGrid
                {
                    ItemsSource = new object[]
                    {
                        new object[] { "Row 1 Col 1: Data data", "Row 1 Col 3: Data dat asdasasda", "Row 1 Col 3: Daewfaefeata data" },
                        new object[] { "Row 2 Col 1: Data ", "Row 2 Col 2: Data data", "Row 2 Col 3: Data data asdfadsfdasdsaf" },
                        new object[] { "Row 3 Col 1: Datadd data", "Row 3 Col 2: Datasdfsfa data", "Row 3 Col 3: Daa" },
                    },
                    ItemTemplate = new DataTemplate(() =>
                    {
                         return new Label
                            {
                                Rotation = -90,
                                HorizontalTextAlignment = TextAlignment.Start,
                                VerticalTextAlignment = TextAlignment.Center,
                                AnchorX=0.5,
                                AnchorY=0.5,
                                HeightRequest = 180,
                                WidthRequest = 180,
                                VerticalOptions = LayoutOptions.Fill,
                                HorizontalOptions = LayoutOptions.Fill,
                                TextColor = Color.Black,
                                Margin = new Thickness(-80,0,-80,0),
                            }.Bind(Label.TextProperty, "Data");
                    }),
                    CellInfo = new CellInfoBinder
                    {
                        Width = -1,
                        Height = -1
                    }
                }
            },
            {
                "MAshed Grid" ,
                () => new BindableGrid
                {
                    ItemsSource = new CC[][]
                    {
                        new CC[] { "This", "ia", "row" },
                        new CC[] { new CC("MASHED",2,2,-1,-1), "LOLDONTCARE", "r567ow" },
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
                    FrozenHeaders = false,
                    ItemsSource = new []
                    {
                        new [] { "This\n is\n fat\n ok", "ia looooooooooooooooooooooooooong", "row" },
                        new [] { "1This", "4ia", "r567ow" },
                        new [] { "32This", "55ia", "ro??w" },
                    },
                    ColumnHeaders = new[]{ new[] { "c1", "c2", "c3 mo mo mo" } },
                    RowHeaders = new[]{ new[] { "r1" }, new[] { "r2\n hai\n hao" }, new[]{ "r3" } },
                    RowHeadersTemplate = new DataTemplate(() =>new Label().Bind(Label.TextProperty, "Data")),
                    ColumnHeadersTemplate = new DataTemplate(() =>new Label().Bind(Label.TextProperty, "Data")),
                    ItemTemplate = new DataTemplate(() =>{
                            return new StackLayout {
                               Children =
                                {
                                    new Label{  BackgroundColor=Color.WhiteSmoke, VerticalOptions=LayoutOptions.Fill, HorizontalOptions=LayoutOptions.Fill}.Bind(Label.TextProperty, "Data") ,
                                  //  new Label{ BackgroundColor = Color.DarkCyan }.Bind(Label.TextProperty, "Data.Below")
                                }
                            };
                        }
                    ),
                    CellInfo = new CellInfoBinder{ Height=-1, Width=-1, ColumnSpan=1, RowSpan=1 }
                }
            },
            {
                "Mashed Headered Grid" ,
                () => new BindableGrid
                {
                    ItemsSource = new CC[][]
                    {
                        new CC[] { "This", "ia", "row" },
                        new CC[] { "1This", new CC("mash",1,2,-1,-1), "no" },
                        new CC[] { "32This", "55ia", "ro??w" },
                    },
                    ColumnHeaders = new CC[][]
                    {
                        new CC[] { new CC("hmash",1,2,-1,-1), "nope", "hai" },
                        new CC[] { "c1", "c2", "c3" }
                    },
                    RowHeaders = new CC[][]
                    {
                        new CC[] { "r1",">" },
                        new CC[] { new CC("r23",2,1,-1,-1),">" },
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
                    var data = Generate(25,22,5,2);
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
                                    .Bind(CellInfoBinder.ColumnSpanProperty, "c")
                                    .Bind(CellInfoBinder.WidthProperty, "w")
                                    .Bind(CellInfoBinder.HeightProperty, "h"),
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
            {
                "grids in row headers frozedn" ,
                () =>
                {
                    var data = Generate(8,6,1,1);
                    return new BindableGrid
                    {
                        FrozenHeaders=true,
                        ColumnHeaders = data.ch,
                        RowHeaders = data.rh,
                        RowHeadersTemplate = new DataTemplate(() =>
                            new Grid
                            {
                                Children =
                                {
                                    new Label{
                                        VerticalTextAlignment = TextAlignment.Center,
                                        HorizontalTextAlignment = TextAlignment.End,
                                        BackgroundColor= Color.Tomato,
                                        TextColor=Color.WhiteSmoke
                                    }.Bind(Label.TextProperty, "Data.val")
                                }
                            }
                        ),
                        ColumnHeadersTemplate = new DataTemplate(() =>
                            new Label{
                                BackgroundColor =Color.DarkCyan,
                                TextColor=Color.WhiteSmoke,
                                VerticalTextAlignment = TextAlignment.End,
                                HorizontalTextAlignment = TextAlignment.Center,
                                VerticalOptions=LayoutOptions.Center,
                                HorizontalOptions = LayoutOptions.Center
                            }.Bind(Label.TextProperty, "Data.val")
                        ),
                        ItemTemplate = new DataTemplate(() =>
                            new Grid
                            {
                                WidthRequest = 50.0,
                                HeightRequest = 50.0,
                                Children=
                                {
                                    new Label{
                                        BackgroundColor=Color.Yellow,
                                        VerticalTextAlignment = TextAlignment.Center,
                                        HorizontalTextAlignment = TextAlignment.Center,
                                        Margin = new Thickness(1,1,0,0) }.Bind(Label.TextProperty, "Data.val")
                                }
                            }
                        ),
                        CellInfo = new CellInfoBinder()
                                    .Bind(CellInfoBinder.RowSpanProperty, "r")
                                    .Bind(CellInfoBinder.ColumnSpanProperty, "c")
                                    .Bind(CellInfoBinder.WidthProperty, "w")
                                    .Bind(CellInfoBinder.HeightProperty, "h"),
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
