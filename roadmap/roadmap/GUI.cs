
    using System;
    using System.Numerics;
    using System.Windows.Forms;
    using Cairo;
    using Gtk;

    namespace roadmap
    {
        public partial class UI : DrawingArea
        {
        public void draw(Context gr)
            {
            gr.SetSourceColor(new Cairo.Color(1, 1, 1, 1));

                //Cursor CursorA = new Cursor(Cursor.Current.Handle);
                //SolidBrush greenBrush = new SolidBrush(System.Drawing.Color.Green);
                PointD start = new PointD(Cursor.Position.X, Cursor.Position.Y);
                PointD end = new PointD(Cursor.Position.X + .1, Cursor.Position.Y + .1);
            gr.MoveTo(start);
            gr.LineTo(end);
            gr.Stroke();
                //gr.Graphics.FillRectangle(greenBrush, Cursor.Position.X, Cursor.Position.Y, 1, 1)/

            }

        }
        public class buttons
        {
        
            static Widget xpm_label_box( string label_text)
            {


                /* Create box for image and label */
                HBox box = new HBox(false, 0);
                box.BorderWidth = 2;

                /* Now on to the image stuff */
                //Gtk.Image image = new Gtk.Image(xpm_filename);

                /* Create a label for the button */
                Gtk.Label label = new Gtk.Label(label_text);

                /* Pack the image and label into the box */
                //box.PackStart(image, false, false, 3);
                box.PackStart(label, false, false, 3);

                //image.Show();
                label.Show();

                return box;
            }
        }
        public class Form1 : Form
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


