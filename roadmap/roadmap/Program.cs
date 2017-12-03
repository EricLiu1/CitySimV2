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
    PictureBox densityMap = new PictureBox();
    PictureBox terrainMap = new PictureBox();
    System.Drawing.Color[,] density;
    System.Drawing.Color[,] terrain;
    public List<Tensor> weightedavgs = new List<Tensor>();


    public void draw(Cairo.Context gr, int width, int height)
    {
        


        gr.Scale(width, height);
        gr.LineWidth = 0.01;
 
        /* draw helping lines */
        gr.SetSourceColor(new Cairo.Color(1, 1, 0, 1));

        List<Edge> streamlines = test();

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
        var position = new Vector2((float)0.5, (float)0.5);

        List<Edge> ans = new List<Edge>();
        Vertex current = new Vertex(position);
        Streamline candidate = new Streamline(current);

        weightedavgs.Add(Tensor.FromRTheta(2, M_PI));

        var mergedistance = 0.01;

        for (int i = 0; i < 100; ++i) 
        {
            Vector2 major = new Vector2();
            Vector2 minor = new Vector2();

            t = new Tensor(0, 0, 0);

            for (int j = 0; j < weightedavgs.Count; ++j) {
                t = new Tensor(weightedavgs[j].Sample().X, weightedavgs[j].Sample().Y, 0) + t;
            }

            t = new Tensor(t.A / weightedavgs.Count, t.B / weightedavgs.Count, 0);

            t.EigenVectors(out major, out minor);
            direction = major;

            //if segment is too small then don't create an edge
            if (direction.Length() < 0.00005f)
                break;

            var temp = new Vector2(current.Position.X + direction.X / 100, current.Position.Y + direction.Y / 100);
            Vertex next = new Vertex(temp);

            Edge myedge = new Edge(candidate, current, next);
            myedge.MakeEdge(current, next, candidate);
            candidate.vertices.Add(current);
            ans.Add(myedge);

            //if segment is out of bounds then add an edge and finish
            if(temp.X > 1.0 || temp.X < 0.0 || temp.Y > 1.0 || temp.Y < 0.0) 
                break;
            
            current = next;

            for (int j = 0; j < weightedavgs.Count; ++j) 
            {
                //recalculate radial tensors
                if(weightedavgs[j].type == 1) {
                    weightedavgs.Remove(weightedavgs[j]);
                    weightedavgs.Add(Tensor.FromXY(temp));
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
        }

        draw(g, w, h);
        g.Dispose();
        return true;
    }

}
