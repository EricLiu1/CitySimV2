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

                //normal colors
                double myR = 0.9;
                double myG = 0.9;
                double myB = 0.9;

                //water colors
                double myR2 = 0.8;
                double myG2 = 0.8;
                double myB2 = 1.0;
                gr.SetSourceColor(new Cairo.Color(myR, myG, myB, 1));
                PointD start2 = new PointD(i/width, 0);
                gr.LineTo(start2);

                PointD end2 = new PointD(i/width, 1/height);
                for (double j = 1; j < height; ++j) 
                {
                    if (terrain[(int)i, (int)j].GetHue() > 60.0f && !water)
                    {
                        gr.LineTo(end2);
                        gr.Stroke();

                        gr.SetSourceColor(new Cairo.Color(myR2, myG2, myB2, 1));
                        start2.Y = j / height;
                        gr.LineTo(start2);
                        end2.Y = j / height;
                        water = true;
                    }
                    else if(terrain[(int)i, (int)j].GetHue() < 60.0f && water) {
                        gr.LineTo(end2);
                        gr.Stroke();

                        gr.SetSourceColor(new Cairo.Color(myR, myG, myB, 1));
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

        }

        gr.LineWidth = 0.005;

        /* draw helping lines */
        gr.SetSourceColor(new Cairo.Color(1, 1, 0, 1));

        List<Edge> streamlines = test();
        //Console.WriteLine(streamlines.Count);
        for (int i = 0; i < streamlines.Count; ++i)
        {
            Edge myedge2 = streamlines[i];
            PointD start = new PointD(myedge2.A.Position.X, myedge2.A.Position.Y);
            PointD end = new PointD(myedge2.B.Position.X, myedge2.B.Position.Y);
            gr.LineTo(start);
            gr.LineTo(end);
            gr.Stroke();

        }
    }

    public void InitializeSeeds() {
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
        var direction = new Vector2(0, 0);
        var position = new Vector2((float)1.0, (float)0.1);
        var position2 = new Vector2((float)0.7, (float)0.7);
        var position3 = new Vector2((float)0.8, (float)0.8);
        List<Vector2> seeds = new List<Vector2>();
        //seeds.Add(position);
        seeds.Add(position2);
        //seeds.Add(position3);

        List<Edge> ans = new List<Edge>();
        //Vertex current = new Vertex(position);
        //Streamline candidate = new Streamline(current);

        //gridline tensrs
        //weightedavgs.Add(Tensor.FromRTheta(2, M_PI));
        //weightedavgs.Add(Tensor.FromRTheta(0.5, M_PI));

        //var mergedistance = 0.01;
        for (int _ = 0; _ < seeds.Count; ++_)
        {
            Vertex current = new Vertex(seeds[_]);

            //radial tensors
            Vector2 center = new Vector2(0.5f, 0.5f);
            //Vector2 center2 = new Vector2(0.2f, 0.9f);
            //Vector2 center3 = new Vector2(0.7f, 0.3f);

            weightedavgs.Add(Tensor.FromXY(seeds[_], center));
            //weightedavgs.Add(Tensor.FromXY(seeds[_], center2));
            //weightedavgs.Add(Tensor.FromXY(seeds[_], center3));

            Streamline candidate = new Streamline(current);

            for (int i = 0; i < 10000; ++i)
            {
                Vector2 major = new Vector2();
                Vector2 minor = new Vector2();

                t = new Tensor(0, 0, 0, new Vector2());

                for (int j = 0; j < weightedavgs.Count; ++j)
                {
                    t = new Tensor(weightedavgs[j].Sample().X, weightedavgs[j].Sample().Y, 0, new Vector2()) + t;
                }

                t = new Tensor(t.A / weightedavgs.Count, t.B / weightedavgs.Count, 0, new Vector2());

                t.EigenVectors(out major, out minor);
                direction = major;

                //if segment is too small then don't create an edge
                if (direction.Length() < 0.000005f)
                {
                    Console.WriteLine(direction.X + " " + direction.Y);
                    Console.WriteLine("hit deadzone");
                    break;
                }

                var temp = new Vector2(current.Position.X + direction.X / 100, current.Position.Y + direction.Y / 100);

                //if segment ends in water then don't create an edge
                int x = (int)(temp.X * width2);
                int y = (int)(temp.Y * height2);
                if (x >= width2)
                    x = width2 - 1;
                if (x < 0)
                    x = 0;
                if (y >= height2)
                    y = height2 - 1;
                if (y < 0)
                    y = 0;
                
                if (terrain[x, y].GetHue() > 60.0f)
                {
                    Console.WriteLine("hit water");
                    break;
                }

                Vertex next = new Vertex(temp);

                Edge myedge = new Edge(candidate, current, next);
                myedge.MakeEdge(current, next, candidate);
                candidate.vertices.Add(current);
                ans.Add(myedge);

                //if segment is out of bounds then add an edge and finish
                if (temp.X > 1.0 || temp.X < 0.0 || temp.Y > 1.0 || temp.Y < 0.0)
                    break;

                current = next;

                for (int j = 0; j < weightedavgs.Count; ++j)
                {
                    //recalculate radial tensors
                    if (weightedavgs[j].type == 1)
                    {
                        Vector2 tensor_center = weightedavgs[j].center2;
                        weightedavgs.Remove(weightedavgs[j]);
                        weightedavgs.Add(Tensor.FromXY(temp, tensor_center));
                    }
                }
            }

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

        if (w != width2 || height2 != h)
        {
            width2 = w;
            height2 = h;
            InitializeSeeds();
            changed = true;
        }

        draw(g, w, h);
        g.Dispose();
        return true;
    }

}
