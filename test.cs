using System;
using System.Windows.Forms;
using System.Drawing;

public class HelloWorld : Form
{
	PictureBox p = new PictureBox();

    static public void Main ()
    {

    	Bitmap img = new Bitmap("temp.png");
    	Color[,] density = new Color[img.Width, img.Height];

    	for(int i = 0; i < img.Width; ++i) {
    		for(int j = 0; j < img.Height; ++j) {
    			Color pixel = img.GetPixel(i, j);
    			density[i,j] = pixel;
    		}
    	}
        Application.Run (new HelloWorld ());
    }

    public HelloWorld ()
    {
    	this.Width = 600;
    	this.Height = 400;
    	p.Image = Image.FromFile("temp.png");
    	p.Size = new Size(600, 400);
    	p.Location = new Point(0, 0);
    	//p.SizeMode = PictureBoxSizeMode.StretchImage;
    	this.Controls.Add(p);

    	Color[,] density2 = new Color[p.Width, p.Height];

    	for(int i = 0; i < p.Width; ++i) {
    		for(int j = 0; j < p.Height; ++j) {
    			Color pixel = GetColorAt(i, j);
    			density2[i,j] = pixel;
    		}
    	}
    }

	private Color GetColorAt(int x, int y) {
	   return ((Bitmap)p.Image).GetPixel(x, y);
	}
}