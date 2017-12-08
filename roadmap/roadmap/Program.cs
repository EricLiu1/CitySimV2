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
using roadmap;

public class GtkCairo
{
    static DrawingArea a;

    static void Main()
    {
        Gtk.Application.Init();
        Window w = new Window("RoadMap gridlines");

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
    static int width2 = 0;
    static int height2 = 0;
    bool changed = true;
    PictureBox densityMap = new PictureBox();
    PictureBox terrainMap = new PictureBox();
    System.Drawing.Color[,] density;
    System.Drawing.Color[,] terrain;
    public List<Tensor> weightedavgs = new List<Tensor>();
    public List<Tensor> polyline;
    public RoadBuilder r = new RoadBuilder();

    public void draw(Context gr, int width, int height)
    {
        //if dimensions are changed redraw the terrain map

        gr.Scale(width, height);

        gr.LineWidth = 0.05;
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

        //find_water();

        gr.LineWidth = 0.005;

        /* draw helping lines */
        gr.SetSourceColor(new Cairo.Color(1, 1, 0, 1));
        r = new RoadBuilder();
        r.InitializeSeeds(new Vector2(0, 0), new Vector2(100, 100));
        HashSet<Edge> all_edges = r.all_edges;
        //Console.WriteLine(streamlines.Count);
        foreach (var e in  all_edges)
        {
            PointD start = new PointD(e.a.X/100, e.a.Y/100);
            PointD end = new PointD(e.b.X/100, e.b.Y/100);
            gr.MoveTo(start);
            gr.LineTo(end);

        }
        gr.Stroke();
    }

    //public void find_water() {
    //    bool[,] dilated_terrain = new bool[width2, height2];

    //    int[] dx = { 1, -1, 0, 0 };
    //    int[] dy = { 0, 0, 1, -1 };
    //    for (var r = 0; r < width2; ++r) 
    //    {
    //        for (var c = 0; c < height2; ++c)
    //        {
    //            if ( c > 0 && terrain[r, c - 1] != terrain[r, c])
    //                dilated_terrain[r, c] = true;
    //            else if ( c < height2 - 1 && terrain[r, c + 1] != terrain[r, c])
    //                dilated_terrain[r, c] = true;
    //            else if (r < width2 - 1 && terrain[r + 1, c] != terrain[r, c])
    //                dilated_terrain[r, c] = true;
    //            else if (r > 0 && terrain[r - 1, c] != terrain[r, c])
    //                dilated_terrain[r, c] = true;
    //            else dilated_terrain[r, c] = false;
    //        }
    //    }

    //    for (var r = 0; r < width2; ++r)
    //    {
    //        for (var c = 0; c < height2; ++c)
    //        {
    //            if (dilated_terrain[r, c])
    //                polyline.AddRange(FollowPath(r, c, dilated_terrain));
    //        }
    //    }

    //}

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

    //public static IEnumerable<Tensor> FollowPath(int start_r, int start_c, bool[,] terrain)
    //{
    //    Vector2 dir = Vector2.Zero;
    //    Vector2 prev = new Vector2(start_r, start_c);
    //    Vector2 curr = new Vector2(start_r, start_c);
    //    int r = start_r, c = start_c;
    //    int[] dx = { 1, 1, 0, -1, -1, -1, 0, 1 };
    //    int[] dy = { 0, -1, -1, -1, 0, 1, 1, 1 };
    //    Queue q = new Queue();
    //    for (var i = 0; i < 8; ++i)
    //    {
    //        if (terrain[r + dx[i], c + dy[i]])
    //            q.Enqueue(new pixel_seed(r + dx[i], c + dy[i], new Vector2(dx[i], dy[i])));
    //    }
    //    while (q.Count > 0)
    //    {
    //        var s = q.Dequeue();
    //        Tensor t = Tensor.FromRTheta(dir.Length(), Math.PI);
           
    //    }
    //     if (dir.Equals(Vector2.Zero)) 
    //        {
                
    //        }


    //    }

    //}


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


    public HashSet<Edge> test() 
    {
        //r.all_edges = new HashSet<Edge>();
        //r.all_vertices = new List<Vector2>();
        //r.tensors = new List<Tensor>();
        //r.streams = new HashSet<Streamline>();
        r = new RoadBuilder();
        r.InitializeSeeds(new Vector2(0, 0), new Vector2(100, 100));
        return r.all_edges;
    }


    protected override bool OnExposeEvent(Gdk.EventExpose args)
    {
        Gdk.Window win = args.Window;
        //Gdk.Rectangle area = args.Area;

        Context g = Gdk.CairoHelper.Create(win);

        int x, y, w, h, d;
        win.GetGeometry(out x, out y, out w, out h, out d);

        //if (w != width2 || height2 != h)
        //{
        //    width2 = w;
        //    height2 = h;
        //    InitializeMaps();
        //    changed = true;
        //}
        width2 = w;
        height2 = h;
        InitializeMaps();
        draw(g, w, h);
        //g.Dispose();
        return true;
    }

}
