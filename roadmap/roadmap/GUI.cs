
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
    using Microsoft.VisualBasic.Devices;
    using System.Windows.Input;
    //Ok so I think none of this currently works.

    namespace roadmap
    {
        public partial class UI : DrawingArea
        {
            public void draw(int width, int height, System.Windows.Forms.PaintEventArgs e)
            {

                //Cursor CursorA = new Cursor(Cursor.Current.Handle);
                SolidBrush greenBrush = new SolidBrush(System.Drawing.Color.Green);
                e.Graphics.FillRectangle(greenBrush, Cursor.Position.X, Cursor.Position.Y, 1, 1);

            }

        }
        public partial class Form1 : Form
        {
            public Form1()
            {
                InitalizeComponent();
            }

            private void InitalizeComponent()
            {
                //add stuff here later
            }

            private void Form1_Load(object sender, EventArgs e)
            {
                System.Windows.Forms.Button button1 = new System.Windows.Forms.Button();
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
            protected override void OnMouseMove(MouseEventArgs e)
            {
                //this.Cursor = new Cursor(Cursor.Current.Handle);
                if (e.Button == MouseButtons.Left)
                {
                    Vector2 mouse = new Vector2(Cursor.Position.X, Cursor.Position.Y);
                    Tensor mouseTensor = new Tensor(0, 0, 0, mouse);
                    //weightedAvgs.add(mouseTensor);

                }

            }
        }
    }


