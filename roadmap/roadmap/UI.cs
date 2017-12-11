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
//Ok so I think none of this currently works.

namespace roadmap
{
    public class UI : DrawingArea
    {
        public void draw(Cairo.Context gr, int width, int height)
        {
            this.Cursor = new Cursor(Cursor.Current.Handle);
            e.Graphics.FillRectangle(gr, Cursor.Position.X, Cursor.Position.Y, 1, 1);
			gr.stroke();
        }
		public void drawTensor()
		{
			this.Cursor = new Cursor(Cursor.Current.Handle);
			if (Mouse.LeftButton == MouseButtonState.Pressed)
			{
				Vector2 mouse = new Vector2(Cursor.Position.X,Cursor.Position.Y);
				Tensor mouseTensor = new Tensor(0,0,0,mouse);
				weightedAvgs.add(mouseTensor);
			}

			
		}
		
    }
	public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            button1.Text = "Submit";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Clicked");
        }
		private void closeButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
