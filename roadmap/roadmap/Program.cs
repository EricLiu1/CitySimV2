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


public class GtkCairo
{
    static DrawingArea a;

    static void Main()
    {
        Application.Init();
        Gtk.Window w = new Gtk.Window("RoadMap gridlines");

        a = new CairoGraphic();

        Box box = new HBox(true, 0);
        box.Add(a);
        w.Add(box);
        w.Resize(500, 500);
        w.ShowAll();

        Application.Run();
    }


}

public class CairoGraphic : DrawingArea
{

    static readonly double M_PI = 3.14159265358979323846;
    static Tensor t;

    static void draw(Cairo.Context gr, int width, int height)
    {

        gr.Scale(width, height);
        gr.LineWidth = 0.01;
 
        /* draw helping lines */
        gr.SetSourceColor(new Color(1, 1, 0, 1));
        gr.LineTo(new PointD(0.5, 0.5));

        List<PointD> streamlines = CairoGraphic.test();
        for (int i = 0; i < streamlines.Count; ++i) {
            Console.WriteLine(streamlines[i].X + " " + streamlines[i].Y);
            gr.LineTo(streamlines[i]);
            gr.Stroke();
            gr.LineTo(streamlines[i]);
        }

        //gr.Arc(xc, yc, radius, angle1, angle1);
        //gr.LineTo(new PointD(xc, yc));
        gr.Stroke();

    }

    public void GenerateTensor() {
        t = new Tensor(5, M_PI);
    }

    public static List<PointD> test() {
        var direction = new Vector2(0, 0);
        var position = new PointD(0.5, 0.5);

        List<PointD> ans = new List<PointD>();
        ans.Add(position);


        for (int i = 0; i < 100; ++i) {
            
            Vector2 coord = new Vector2( (float)position.X, (float)position.Y);
            Tensor t = Tensor.FromXY(coord);
            Vector2 major = new Vector2();
            Vector2 minor = new Vector2();

            Random random = new Random();
            t.EigenVectors(out major, out minor);
            //direction = new Vector2((float) random.NextDouble() * major.X, (float) random.NextDouble() * major.Y);
            direction = major;
            if (direction.Length() < 0.00005f)
                break;
            
            position = new PointD(position.X + direction.X, position.Y + direction.Y);
            ans.Add(position);
        }

        return ans;
    }


    protected override bool OnExposeEvent(Gdk.EventExpose args)
    {
        Gdk.Window win = args.Window;
        //Gdk.Rectangle area = args.Area;

        Cairo.Context g = Gdk.CairoHelper.Create(win);

        int x, y, w, h, d;
        win.GetGeometry(out x, out y, out w, out h, out d);

        draw(g, w, h);
        return true;
    }

}
