using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using unvell.D2DLib;
using unvell.D2DLib.WinForm;

namespace SharpLabs
{
    public static class Globals
    {
        public static float accuracy = 5f;
        public static float gravity = 400f;
        public static int clothY = 34;
        public static int clothX = 44;
        public static int spacing = 8;
        public static int tearDist = 60;
        public static float friction = 0.99f;
        public static float bounce = 0.50f;
        public static int WIDTH = 800;
        public static int HEIGHT = 600;
        public static MouseInfo mouse = new MouseInfo();
    }
    public class MouseInfo
    {
        public int cut = 8;
        public int influence = 36;
        public bool down = false;
        public int button = 1;
        public int x = 0;
        public int y = 0;
        public int px = 0;
        public int py = 0;
    }

    public class PointInfo
    {
        public float x;
        public float y;
        public float px;
        public float py;
        public float vx;
        public float vy;
        public float pinX;
        public float pinY;
        public List<Constraint> constraints;
        

        public PointInfo(float x_,float y_)
        {
            x = x_;
            y = y_;
            px = x_;
            py = y_;
            vx = 0;
            vy = 0;
            pinX = 0;
            pinY = 0;
            constraints = new List<Constraint>();
        }

        public PointInfo update(float delta)
        {
            var mouse = Globals.mouse;

            if (pinX!=0 && pinY!=0) return this;


            if (mouse.down)
            {
                var dx = x - mouse.x;
                var dy = y - mouse.y;
                var dist = Math.Sqrt(dx * dx + dy * dy);

                if (mouse.button == 1 && dist < mouse.influence)
                {
                    px = x - (mouse.x - mouse.px);
                    py = y - (mouse.y - mouse.py);
                } else if (dist < mouse.cut)
                {
                    constraints = new List<Constraint>();
                }
            }

            addForce(0, Globals.gravity);

            var nx = x + (x - px) * Globals.friction + vx * delta;
            var ny = y + (y - py) * Globals.friction + vy * delta;

            px = x;
            py = y;

            x = nx;
            y = ny;

            vy = 0;
            vx = 0;

            if (x >= Globals.WIDTH)
            {
                px = Globals.WIDTH + (Globals.WIDTH - px) * Globals.bounce;
                x = Globals.WIDTH;
            }else if (x <= 0)
            {
                px *= -1 * Globals.bounce;
                x = 0;
            }

            if (y >= Globals.HEIGHT)
            {
                py = Globals.HEIGHT + (Globals.HEIGHT - py) * Globals.bounce;
                y = Globals.HEIGHT;
            }else if (y <= 0)
            {
                py *= -1 * Globals.bounce;
                y = 0;
            }

            

            return this;
            
        }


        public void draw(D2DGraphics g)
        {
            var i = constraints.Count;
            while (i-- > 0)
            {
                constraints[i].draw(g);
            }
        }

        public void resolve()
        {
            if (pinX!=0 && pinY!=0)
            {
                x = pinX;
                y = pinY;
                return;
            }

            foreach (var c in constraints.ToList())
            {
                c.resolve();
            }
        }

        public void attach(PointInfo point)
        {
            constraints.Add(new Constraint(this, point));
        }

        public void free(Constraint constraint)
        {
            constraints.Remove(constraint);
        }

        public void addForce(float x, float y)
        {
            vx += x;
            vy += y;
        }

        public void pin(float pinx, float piny)
        {
            pinX = pinx;
            pinY = piny;
        }

        
    }


    public class Constraint
    {
        public PointInfo p1;
        public PointInfo p2;
        public int length;

        public Constraint(PointInfo p1, PointInfo p2)
        {
            this.p1 = p1;
            this.p2 = p2;
            length = Globals.spacing;
        }

        public void resolve()
        {
            var dx = p1.x - p2.x;
            var dy = p1.y - p2.y;
            var dist = Math.Sqrt(dx * dx + dy * dy);

            if (dist < length) return;

            var diff = (length - dist) / dist;

            if (dist > Globals.tearDist)
            {
                p1.free(this);
            }

            float mul = (float)diff * 0.5f * (1 - length / (float)dist);

            float px = dx * mul;
            float py = dy * mul;

            p1.x += px;
            p1.y += py;
            p2.x -= px;
            p2.y -= py;

        }

        public void draw(D2DGraphics g)
        {
            g.DrawLine(p1.x, p1.y, p2.x, p2.y, D2DColor.Black);
        }

    }



    public partial class MainForm : D2DForm
    {

        public List<PointInfo> points = new List<PointInfo>();
        public int startX = Globals.WIDTH / 2 - Globals.clothX * Globals.spacing / 2;
        Timer t = new Timer();


        public MainForm()
        {
            InitializeComponent();
            this.ShowFPS = true;
            this.AnimationDraw = true;

            this.ClientSize = new Size(Globals.WIDTH, Globals.HEIGHT);

            var free = false; // ?!?!?!


            for (int y = 0; y <= Globals.clothY; y++)
            {
                for (int x = 0; x <= Globals.clothX; x++)
                {
                    var point = new PointInfo(startX + x * Globals.spacing, 20 + y * Globals.spacing);

                    if (!free && y==0)
                    {
                        point.pin(point.x, point.y);
                    }
                    else
                    {

                    }

                    if (x != 0)
                    {
                        point.attach(points.Last());
                    }
                    
                    if (y != 0)
                    {
                        point.attach(points[x + (y - 1) * (Globals.clothX + 1)]);
                    }

                    points.Add(point);
                }
            }

            //t.Interval = 16; // 60fps
            //t.Tick += T_Tick;
            //t.Enabled = true;
        }

        private void T_Tick(object sender, EventArgs e)
        {
            this.Invalidate();
        }

        public void update(float delta,D2DGraphics g)
        {
            var i = Globals.accuracy;

            while(i-- > 0)
            {
                foreach (var p in points)
                {
                    p.resolve();
                }
            }

            foreach (var p in points)
            {
                p.update(delta * delta).draw(g);
            }
        }

        protected override void OnFrame()
        {
            

        }

        protected override void OnRender(D2DGraphics g)
        {
            g.FillRectangle(0, 0, this.Width, this.Height, D2DColor.GhostWhite);
            update(0.016f,g);

            SceneChanged = true;
        }


        public void setMouse(MouseEventArgs e)
        {
            Globals.mouse.px = Globals.mouse.x;
            Globals.mouse.py = Globals.mouse.y;
            Globals.mouse.x = e.X;
            Globals.mouse.y = e.Y;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {

            
            setMouse(e);

            //this.Invalidate();
        }


        protected override void OnMouseDown(MouseEventArgs e)
        {
            Globals.mouse.down = true;
            if (e.Button == MouseButtons.Left)
            {
                Globals.mouse.button = 1;
            }
            else if (e.Button == MouseButtons.Right)
            {
                Globals.mouse.button = 2;
            }
            else
            {
                Globals.mouse.button = 1;
            }

            setMouse(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            Globals.mouse.down = false;
            setMouse(e);
        }
    }
}
