//
//
//  Mono.Cairo drawing samples using GTK# as drawing surface
//  Autor: Jordi Mas <jordi@ximian.com>. Based on work from Owen Taylor
//         Hisham Mardam Bey <hisham@hisham.cc>
//

//
// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Reflection;
using System.Collections;
using System.Numerics;
using System.Runtime.InteropServices;
using Cairo;
using Gtk;
using System.Collections.Generic;
using System.Drawing;
using roadmap;

public class GtkCairo
{
    static CairoGraphic map;
    static void Main()
    {
        
        Gtk.Application.Init();
        Window MainWindow = new Window("RoadMap gridlines");
        map = new CairoGraphic();
        Box hbox = new HBox(false, 5);
        hbox.Allocation = new Gdk.Rectangle(new Gdk.Point(0, 0), new Gdk.Size(1200, 800));

        Box settings = new VBox(true, 0);
        settings.Allocation = new Gdk.Rectangle(new Gdk.Point(0, 0), new Gdk.Size(400, 800));

        Label mylabel = new Label();
        mylabel.Text = "Settings";
        mylabel.Allocation = new Gdk.Rectangle(new Gdk.Point(0, 0), new Gdk.Size(390, 50));

        settings.PackStart(mylabel, false, false, 5);

        Button start = new Button("Start");
        start.Clicked += (sender, EventArgs) => { map.startClicked(sender, EventArgs); };
        start.Allocation = new Gdk.Rectangle(new Gdk.Point(0, 0), new Gdk.Size(185, 100));
        settings.PackStart(start, true, true, 5);

        Button reset = new Button("Reset");
        reset.Clicked += (sender, EventArgs) => { map.resetClicked(sender, EventArgs); };
        reset.Allocation = new Gdk.Rectangle(new Gdk.Point(0, 0), new Gdk.Size(185, 100));
        settings.PackStart(reset, true, true, 5);





        hbox.PackStart(map, true, true, 0);
        hbox.PackStart(settings, false, false, 0);

       
        MainWindow.Add(hbox);
        MainWindow.Resize(1200, 800);
        MainWindow.ShowAll();
        map.myWindow = MainWindow.GdkWindow;
        Gtk.Application.Run();
    }

}

public class MyEventArgs : EventArgs {
    public Gdk.Window win { get; }
}

public delegate void MyEventHandler(MyEventArgs e);

public class CairoGraphic : DrawingArea
{
    static int width = 0;
    static int height = 0;
    public List<Tensor> weightedavgs = new List<Tensor>();
    public List<Tensor> polyline;
    public RoadBuilder r = new RoadBuilder();
    public Gdk.Window myWindow;
    public Context g;
    public bool first_time = true;

    public void draw(Context gr)
    {
        gr.Scale(width, height);
        r.setDimensions(width, height);

        /* Draw Terrain */
        gr.LineWidth = 0.05;
        drawTerrain(gr);

        /* Draw Highways */
        gr.LineWidth = 0.005;
        drawHighways(gr);

    }

    public void drawTerrain(Context gr)
    {
        Cairo.Color normal_color = new Cairo.Color(0.9, 0.9, 0.9, 1);
        Cairo.Color water_color = new Cairo.Color(0.8, 0.8, 1.0, 1);

        for (double i = 0; i < width; ++i)
        {
            int k = (int)i;
            bool water = false;

            gr.SetSourceColor(normal_color);
            PointD start2 = new PointD(i / width, 0);
            gr.MoveTo(start2);

            PointD end2 = new PointD(i / width, 1 / height);
            for (double j = 1; j < height; ++j)
            {
                if (r.GetColor((int)i, (int)j).R == 0 && !water)
                {
                    gr.LineTo(end2);
                    gr.Stroke();
                    gr.SetSourceColor(water_color);
                    start2.Y = j / height;
                    gr.MoveTo(start2);
                    end2.Y = j / height;
                    water = true;
                }
                else if (r.GetColor((int)i, (int)j).R > 0 && water)
                {
                    gr.LineTo(end2);
                    gr.Stroke();
                    gr.SetSourceColor(normal_color);
                    start2.Y = j / height;
                    gr.MoveTo(start2);
                    end2.Y = j / height;
                    water = false;
                }
                else
                {
                    end2.Y = j / height;
                }
            }
            gr.LineTo(end2);
            gr.Stroke();

        }

    }

    public void drawHighways(Context gr)
    {
        
        gr.SetSourceColor(new Cairo.Color(1, 1, 0, 1));
        HashSet<Edge> all_edges = getStreamlines();
        foreach (var e in all_edges)
        {
            PointD start = new PointD(e.a.X / width, e.a.Y / height);
            PointD end = new PointD(e.b.X / width, e.b.Y / height);
            gr.MoveTo(start);
            gr.LineTo(end);
        }
        gr.Stroke();

        PointD start2 = new PointD(0, 0);
        PointD end2 = new PointD(1, 1);
        gr.SetSourceColor(new Cairo.Color(1, 1, 0, 1));
        gr.MoveTo(start2);
        gr.LineTo(end2);
        gr.Stroke();
    }

    internal struct pixel_seed
    {
        public int r;
        public int c;
        public Vector2 dir;

        public pixel_seed(int tr, int tc, Vector2 tdir)
        {
            r = tr;
            c = tc;
            dir = tdir;
        }
    }

    public HashSet<Edge> getStreamlines()
    {
        return r.all_edges;
    }

    protected override bool OnExposeEvent(Gdk.EventExpose args)
    {
        if (first_time)
        {
            myWindow = args.Window;

            first_time = false;
        }

        g = Gdk.CairoHelper.Create(myWindow);
        int x, y, w, h, d;
        myWindow.GetGeometry(out x, out y, out w, out h, out d);
        width = 800;
        height = 800;
        draw(g);
        g.Dispose();
        //    draw_clicked = false;
        //}

        return true;
    }

    public void startClicked(object sender, EventArgs args)
    {
        r.InitializeSeeds(new Vector2(0, 0), new Vector2(width, height));
    }

    public void resetClicked(object sender, EventArgs args) {
        r.all_edges.Clear();
    }
}
