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

    static int width2 = 0;
    static int height2 = 0;
    bool changed = true;
    PictureBox densityMap = new PictureBox();
    PictureBox terrainMap = new PictureBox();
    System.Drawing.Color[,] density;
    System.Drawing.Color[,] terrain;
    public List<Tensor> weightedavgs = new List<Tensor>();
    public List<Vector2> water_bounds;
    public roadmap.RoadBuilder r = new roadmap.RoadBuilder();

    public void draw(Cairo.Context gr, int width, int height)
    {
        //if dimensions are changed redraw the terrain map
        if(changed) {
            changed = false;
            gr.Scale(width, height);

            gr.LineWidth = 0.005;
            for (double i = 0; i < width; ++i) 
            {
                int k = (int)i;
                bool water = false;

                //normal color
                Cairo.Color normal_color = new Cairo.Color(0.9, 0.9, 0.9, 1);
                Cairo.Color water_color = new Cairo.Color(0.8, 0.8, 1.0, 1);

                gr.SetSourceColor(normal_color);
                PointD start2 = new PointD(i/width, 0);
                gr.LineTo(start2);

                PointD end2 = new PointD(i/width, 1/height);
                for (double j = 1; j < height; ++j) 
                {
                    if (terrain[(int)i, (int)j].GetHue() > 60.0f && !water)
                    {
                        gr.LineTo(end2);
                        gr.Stroke();

                        gr.SetSourceColor(water_color);
                        start2.Y = j / height;
                        gr.LineTo(start2);
                        end2.Y = j / height;
                        water = true;
                    }
                    else if(terrain[(int)i, (int)j].GetHue() < 60.0f && water) {
                        gr.LineTo(end2);
                        gr.Stroke();

                        gr.SetSourceColor(normal_color);
                        start2.Y = j / height;
                        gr.LineTo(start2);
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

            find_water();
        }

        gr.LineWidth = 0.005;

        /* draw helping lines */
        gr.SetSourceColor(new Cairo.Color(1, 1, 0, 1));

        List<Edge> streamlines = test();
        Console.WriteLine(streamlines.Count);
        for (int i = 0; i < streamlines.Count; ++i)
        {
            Edge myedge2 = streamlines[i];
            PointD start = new PointD(myedge2.a.pos.X, myedge2.a.pos.Y);
            PointD end = new PointD(myedge2.b.pos.X, myedge2.b.pos.Y);
            gr.LineTo(start);
            gr.LineTo(end);
            gr.Stroke();

        }
    }

    public void find_water() {
        water_bounds = new List<Vector2>();

        for (int i = 0; i < width2; ++i)
        {
            for (int j = 0; j < height2; ++j)
            {

                if (edge_of_water(i, j) && terrain[i, j].GetHue() > 60.0)
                    water_bounds.Add(new Vector2(i, j));
            }
        }
    }

    public bool edge_of_water(int x, int y) {
        
        if(x > 0) {
            if (terrain[x - 1, y].GetHue() < 60.0) 
                return true;
        }

        if(x < width2 - 1) {
            if (terrain[x + 1, y].GetHue() < 60.0)
                return true;
        }

        if(y > 0) {
            if (terrain[x, y - 1].GetHue() < 60.0)
                return true;
        }

        if(y < height2 - 1) {
            if (terrain[x, y + 1].GetHue() < 60.0)
                return true;
        }

        return false;
    }
    public void InitializeMaps()
    {
        densityMap.Image = System.Drawing.Image.FromFile("density_map.png");
        densityMap.Size = new Size(width2, height2);
        densityMap.SizeMode = PictureBoxSizeMode.StretchImage;
        densityMap.Location = new System.Drawing.Point(0, 0);
        density = new System.Drawing.Color[densityMap.Width, densityMap.Height];

        terrainMap.Image = System.Drawing.Image.FromFile("terrain_map.png");
        terrainMap.Size = new Size(width2, height2);
        terrainMap.SizeMode = PictureBoxSizeMode.StretchImage;
        terrainMap.Location = new System.Drawing.Point(0, 0);
        terrain = new System.Drawing.Color[terrainMap.Width, terrainMap.Height];

        Bitmap img = (Bitmap)densityMap.Image;
        Bitmap img2 = (Bitmap)terrainMap.Image;
        float stretch_X = img.Width / (float)densityMap.Width;
        float stretch_Y = img.Height / (float)densityMap.Height;
        float stretch_X2 = img2.Width / (float)terrainMap.Width;
        float stretch_Y2 = img2.Height / (float)terrainMap.Height;

        for (int i = 0; i < densityMap.Width; ++i)
        {
            for (int j = 0; j < densityMap.Height; ++j)
            {
                density[i, j] = img.GetPixel((int)(i * stretch_X), (int)(j * stretch_Y));
                terrain[i, j] = img2.GetPixel((int)(i * stretch_X2), (int)(j * stretch_Y2));
            }
        }
    }


    public List<Edge> test() 
    {
        r.all_edges = new List<Edge>();
        r.all_vertices = new List<Vertex>();
        r.tensors = new List<Tensor>();
        r.streams = new HashSet<Streamline>();
        r.InitializeSeeds(new Vector2(0, 0), new Vector2(1, 1));
        return r.all_edges;
    }


    protected override bool OnExposeEvent(Gdk.EventExpose args)
    {
        Gdk.Window win = args.Window;
        //Gdk.Rectangle area = args.Area;

        Context g = Gdk.CairoHelper.Create(win);

        int x, y, w, h, d;
        win.GetGeometry(out x, out y, out w, out h, out d);

        if (w != width2 || height2 != h)
        {
            width2 = w;
            height2 = h;
            InitializeMaps();
            changed = true;
        }

        draw(g, w, h);
        g.Dispose();
        return true;
    }

}
