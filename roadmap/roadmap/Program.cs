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
using System.Windows.Forms;
using Cairo;
using Gtk;
using System.Collections.Generic;
using System.Drawing;

public class GtkCairo
{
    static DrawingArea a;

    static void Main()
    {
        Gtk.Application.Init();
        Gtk.Window w = new Gtk.Window("RoadMap gridlines");

        a = new CairoGraphic();

        Box box = new HBox(true, 0);
        box.Add(a);
        w.Add(box);
        w.Resize(500, 500);
        w.ShowAll();

        Gtk.Application.Run();
    }


}

public class CairoGraphic : DrawingArea
{

    static readonly double M_PI = 3.14159265358979323846;
    static Tensor t;
    static bool p = false;

    static int width2 = 0;
    static int height2 = 0;
    PictureBox boundaryMap = new PictureBox();
    PictureBox terrainMap = new PictureBox();
    System.Drawing.Color[,] density;
    System.Drawing.Color[,] terrain;

    static void draw(Cairo.Context gr, int width, int height)
    {
        


        gr.Scale(width, height);
        gr.LineWidth = 0.01;
 
        /* draw helping lines */
        gr.SetSourceColor(new Cairo.Color(1, 1, 0, 1));


        gr.LineTo(new PointD(0.5, 0.5));

        List<PointD> streamlines = CairoGraphic.test();
        for (int i = 0; i < streamlines.Count; ++i)
        {
            Console.WriteLine(streamlines[i].X + " " + streamlines[i].Y);
            gr.LineTo(streamlines[i]);
            gr.Stroke();
            gr.LineTo(streamlines[i]);
        }
    }

    public void InitializeSeeds()
    {
        //boundaryMap.Image = System.Drawing.Image.FromFile("boundary_map.png");
        //boundaryMap.Size = new Size(width2, height2);
        //boundaryMap.Location = new System.Drawing.Point(0, 0);
        //density = new System.Drawing.Color[boundaryMap.Width, boundaryMap.Height];

        //for (int i = 0; i < boundaryMap.Width; ++i)
        //{
        //    for (int j = 0; j < boundaryMap.Height; ++j)
        //    {
        //        density[i, j] = GetColorAt(i, j, boundaryMap);
        //    }
        //}

        //terrainMap.Image = System.Drawing.Image.FromFile("terrain_map.png");
        //terrainMap.Size = new Size(width2, height2);
        //terrainMap.Location = new System.Drawing.Point(0, 0);
        //terrain = new System.Drawing.Color[boundaryMap.Width, boundaryMap.Height];

        //for (int i = 0; i < terrainMap.Width; ++i) {
        //    for (int j = 0; j < terrainMap.Height; ++j) {
        //        terrain[i, j] = GetColorAt(i, j, terrainMap);
        //    }
        //}

    }

    public static List<PointD> test() 
    {
        var direction = new Vector2(0, 0);
        var position = new PointD(0.5, 0.5);

        List<PointD> ans = new List<PointD>();
        ans.Add(position);
        t = Tensor.FromRTheta(2, M_PI);


        for (int i = 0; i < 100; ++i) {
            
            Vector2 coord = new Vector2( (float)position.X, (float)position.Y);

            //Tensor t = Tensor.FromXY(coord);
            Vector2 major = new Vector2();
            Vector2 minor = new Vector2();

            Random random = new Random();
            t.EigenVectors(out major, out minor);
            direction = major * .01f;
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

        if (!p)
        {
            width2 = w;
            height2 = h;
            InitializeSeeds();
            p = true;
        }

        draw(g, w, h);
        return true;
    }

    public System.Drawing.Color GetColorAt(int x, int y, PictureBox p)
    {
        return ((Bitmap)p.Image).GetPixel(x, y);
    }
}
