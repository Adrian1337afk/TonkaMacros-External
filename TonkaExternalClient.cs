using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

[assembly:System.Reflection.AssemblyTitle("Tonka External Client")]
[assembly:System.Reflection.AssemblyDescription("External Windows-input macro client")]
[assembly:System.Reflection.AssemblyVersion("4.3.0.0")]
[assembly:System.Reflection.AssemblyFileVersion("4.3.0.0")]

namespace TonkaExternalClient {
    static class UiTheme {
        public static Color Accent=Color.FromArgb(92,205,252);
        public static Color Text=Color.FromArgb(240,244,248);
        public static readonly Color Bg=Color.FromArgb(14,17,21),Surface=Color.FromArgb(18,22,27),Card=Color.FromArgb(23,28,34),Border=Color.FromArgb(37,44,53),Divider=Color.FromArgb(33,40,48);
        public static readonly Color Field=Color.FromArgb(15,19,24),FieldBorder=Color.FromArgb(44,53,63),Muted=Color.FromArgb(139,152,166),Subtle=Color.FromArgb(96,109,123),Good=Color.FromArgb(78,214,160),Warn=Color.FromArgb(255,138,101);
        public static Color AccentSoft{get{return Color.FromArgb(42,Accent.R,Accent.G,Accent.B);}}
        public static Color AccentDark{get{return Blend(Card,Accent,.2f);}}
        public static Color OnAccent{get{return (Accent.R*299+Accent.G*587+Accent.B*114)/1000>150?Color.FromArgb(12,16,20):Color.White;}}
        public static Color Blend(Color a,Color b,float t){return Color.FromArgb((int)(a.R+(b.R-a.R)*t),(int)(a.G+(b.G-a.G)*t),(int)(a.B+(b.B-a.B)*t));}
        public static GraphicsPath Round(RectangleF r,float radius){var p=new GraphicsPath();float d=Math.Min(radius*2,Math.Min(r.Width,r.Height));if(d<=0){p.AddRectangle(r);return p;}p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
        public static void Fill(Graphics g,Color c,RectangleF r,float radius){g.SmoothingMode=SmoothingMode.AntiAlias;using(var p=Round(r,radius))using(var b=new SolidBrush(c))g.FillPath(b,p);}
        public static void Stroke(Graphics g,Color c,RectangleF r,float radius){g.SmoothingMode=SmoothingMode.AntiAlias;using(var p=Round(r,radius))using(var pen=new Pen(c))g.DrawPath(pen,p);}
        public static void Write(Graphics g,string s,Font f,Rectangle r,Color c,TextFormatFlags align){TextRenderer.DrawText(g,s,f,r,c,align|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.NoPadding);}
    }

    // Base for owner-drawn controls: transparent over the parent, double-buffered, tracks hover.
    class PaintedControl:Control {
        protected bool Hover;
        public PaintedControl(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.SupportsTransparentBackColor|ControlStyles.ResizeRedraw,true);SetStyle(ControlStyles.StandardDoubleClick|ControlStyles.Selectable,false);BackColor=Color.Transparent;}
        protected override void OnMouseEnter(EventArgs e){Hover=true;Invalidate();base.OnMouseEnter(e);}
        protected override void OnMouseLeave(EventArgs e){Hover=false;Invalidate();base.OnMouseLeave(e);}
        protected override void OnTextChanged(EventArgs e){Invalidate();base.OnTextChanged(e);}
    }

    sealed class SoftButton:PaintedControl {
        public bool Selected,Primary,Danger;public string PreviewFont;
        public SoftButton(){Cursor=Cursors.Hand;Size=new Size(120,30);}
        protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;var r=new RectangleF(.5f,.5f,Width-1.5f,Height-1.5f);Color fill,border,text;
            if(Danger){fill=UiTheme.Blend(UiTheme.Surface,UiTheme.Warn,Hover?.2f:.12f);border=UiTheme.Warn;text=UiTheme.Warn;}
            else if(Primary){fill=Hover?UiTheme.Blend(UiTheme.Accent,Color.White,.12f):UiTheme.Accent;border=fill;text=UiTheme.OnAccent;}
            else if(Selected){fill=UiTheme.AccentDark;border=UiTheme.Accent;text=UiTheme.Text;}
            else{fill=Hover?UiTheme.Blend(UiTheme.Field,Color.White,.05f):UiTheme.Field;border=Hover?UiTheme.Blend(UiTheme.FieldBorder,Color.White,.12f):UiTheme.FieldBorder;text=Hover?UiTheme.Text:Color.FromArgb(178,189,201);}
            UiTheme.Fill(g,fill,r,7);UiTheme.Stroke(g,border,r,7);
            Font f=Font;bool own=false;if(PreviewFont!=null){try{f=new Font(PreviewFont,Font.Size,Font.Style);own=true;}catch{}}
            UiTheme.Write(g,Text,f,ClientRectangle,text,TextFormatFlags.HorizontalCenter|TextFormatFlags.EndEllipsis);if(own)f.Dispose();}
    }

    sealed class NavItem:PaintedControl {
        public bool Selected;readonly string icon;static readonly Font IconFont=new Font("Segoe MDL2 Assets",11F);
        public NavItem(string icon,string text){this.icon=icon;Text=text;Cursor=Cursors.Hand;}
        protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;var r=new RectangleF(0,0,Width-1,Height-1);
            if(Selected)UiTheme.Fill(g,UiTheme.Blend(UiTheme.Surface,UiTheme.Accent,.13f),r,8);else if(Hover)UiTheme.Fill(g,UiTheme.Blend(UiTheme.Surface,Color.White,.04f),r,8);
            if(Selected)UiTheme.Fill(g,UiTheme.Accent,new RectangleF(0,Height/2f-9,3,18),1.5f);
            UiTheme.Write(g,icon,IconFont,new Rectangle(14,0,24,Height),Selected?UiTheme.Accent:UiTheme.Subtle,TextFormatFlags.HorizontalCenter);
            UiTheme.Write(g,Text,Font,new Rectangle(48,0,Width-52,Height),Selected?UiTheme.Text:Hover?Color.FromArgb(205,212,220):UiTheme.Muted,TextFormatFlags.Left);}
    }

    sealed class WindowButton:PaintedControl {
        readonly bool close;
        public WindowButton(bool close){this.close=close;Cursor=Cursors.Hand;}
        protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;if(Hover)using(var b=new SolidBrush(close?Color.FromArgb(196,43,28):Color.FromArgb(34,40,47)))g.FillRectangle(b,ClientRectangle);g.SmoothingMode=SmoothingMode.AntiAlias;int cx=Width/2,cy=Height/2;using(var pen=new Pen(Hover?Color.White:UiTheme.Muted,1.2f)){if(close){g.DrawLine(pen,cx-5,cy-5,cx+5,cy+5);g.DrawLine(pen,cx+5,cy-5,cx-5,cy+5);}else g.DrawLine(pen,cx-5,cy,cx+5,cy);}}
    }

    sealed class StatusPill:PaintedControl {
        public Color DotColor=UiTheme.Subtle;public string Key;
        protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;var r=new RectangleF(.5f,.5f,Width-1.5f,Height-1.5f);UiTheme.Fill(g,UiTheme.Blend(UiTheme.Surface,DotColor,.1f),r,Height/2f);UiTheme.Stroke(g,UiTheme.Blend(UiTheme.Surface,DotColor,.28f),r,Height/2f);int x=10;
            if(Key!=null){int w=TextRenderer.MeasureText(Key,Font).Width+8;var k=new Rectangle(8,4,w,Height-9);UiTheme.Fill(g,UiTheme.Field,k,4);UiTheme.Stroke(g,UiTheme.Blend(UiTheme.FieldBorder,DotColor,.35f),k,4);UiTheme.Write(g,Key,Font,k,DotColor,TextFormatFlags.HorizontalCenter);x=k.Right+8;}
            else{UiTheme.Fill(g,DotColor,new RectangleF(x,Height/2f-3,6,6),3);x+=13;}
            UiTheme.Write(g,Text,Font,new Rectangle(x,0,Width-x-6,Height),DotColor,TextFormatFlags.Left);}
    }

    sealed class Badge:PaintedControl {
        protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;var r=new RectangleF(0,0,Width-1,Height-1);UiTheme.Fill(g,UiTheme.AccentDark,r,9);UiTheme.Stroke(g,UiTheme.Blend(UiTheme.AccentDark,UiTheme.Accent,.35f),r,9);UiTheme.Write(g,Text,Font,ClientRectangle,UiTheme.Accent,TextFormatFlags.HorizontalCenter);}
    }

    sealed class Swatch:PaintedControl {
        public readonly Color Value;readonly bool selected;
        public Swatch(Color value,bool selected){Value=value;this.selected=selected;Cursor=Cursors.Hand;}
        protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;float inset=selected||Hover?4:1;using(var b=new SolidBrush(Value))g.FillEllipse(b,inset,inset,Width-1-inset*2,Height-1-inset*2);if(selected||Hover)using(var pen=new Pen(selected?UiTheme.Text:UiTheme.FieldBorder,1.6f))g.DrawEllipse(pen,1,1,Width-3,Height-3);}
    }

    sealed class Slider:PaintedControl {
        public int Minimum,Maximum,Value;public event Action<int> ValueChanged;bool drag;
        public Slider(){Cursor=Cursors.Hand;}
        float KnobX{get{return 8+(Width-16)*(Value-Minimum)/(float)Math.Max(1,Maximum-Minimum);}}
        protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;float cy=Height/2f,x=KnobX;UiTheme.Fill(g,UiTheme.FieldBorder,new RectangleF(8,cy-2,Width-16,4),2);UiTheme.Fill(g,UiTheme.Accent,new RectangleF(8,cy-2,x-8,4),2);using(var b=new SolidBrush(UiTheme.Text))g.FillEllipse(b,x-7,cy-7,14,14);using(var pen=new Pen(UiTheme.Accent,2f))g.DrawEllipse(pen,x-7,cy-7,14,14);}
        void SetFrom(int px){int v=Minimum+(int)Math.Round((px-8)*(Maximum-Minimum)/(double)Math.Max(1,Width-16));v=Math.Max(Minimum,Math.Min(Maximum,v));if(v==Value)return;Value=v;Invalidate();if(ValueChanged!=null)ValueChanged(v);}
        protected override void OnMouseDown(MouseEventArgs e){drag=true;SetFrom(e.X);base.OnMouseDown(e);}
        protected override void OnMouseMove(MouseEventArgs e){if(drag)SetFrom(e.X);base.OnMouseMove(e);}
        protected override void OnMouseUp(MouseEventArgs e){drag=false;base.OnMouseUp(e);}
    }

    // Sequence footer on each card: steps drawn as pills joined by chevrons, wrapping to the card width.
    sealed class FlowStrip:PaintedControl {
        readonly string[] steps;readonly List<Rectangle> boxes=new List<Rectangle>();
        public FlowStrip(string text,int width,Font font){steps=text.Split(new[]{" > "},StringSplitOptions.RemoveEmptyEntries);Font=font;Width=width;Height=Arrange();}
        int Arrange(){boxes.Clear();int x=0,y=0;const int h=22,gap=18;foreach(var s in steps){int w=TextRenderer.MeasureText(s,Font).Width+14;if(x>0&&x+w>Width){x=0;y+=h+6;}boxes.Add(new Rectangle(x,y,w,h));x+=w+gap;}return y+h+1;}
        protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;for(int i=0;i<boxes.Count;i++){var b=boxes[i];UiTheme.Fill(g,UiTheme.Blend(UiTheme.Card,UiTheme.Accent,.12f),new RectangleF(b.X,b.Y,b.Width-1,b.Height-1),b.Height/2f);UiTheme.Write(g,steps[i],Font,b,UiTheme.Accent,TextFormatFlags.HorizontalCenter);
            if(i+1<boxes.Count&&boxes[i+1].Y==b.Y){g.SmoothingMode=SmoothingMode.AntiAlias;float cx=b.Right+8,cy=b.Y+b.Height/2f;using(var pen=new Pen(UiTheme.Subtle,1.4f))g.DrawLines(pen,new[]{new PointF(cx-2,cy-4),new PointF(cx+2,cy),new PointF(cx-2,cy+4)});}}}
    }

    // Thin overlay scrollbar that drives a panel whose native scrollbar is clipped out of view.
    sealed class ScrollRail:PaintedControl {
        readonly ScrollableControl target;bool drag;int grabY,grabScroll;
        public ScrollRail(ScrollableControl target){this.target=target;}
        int Total{get{return Math.Max(1,target.DisplayRectangle.Height);}}int View{get{return target.ClientSize.Height;}}int Offset{get{return -target.AutoScrollPosition.Y;}}
        Rectangle Thumb{get{if(Total<=View)return Rectangle.Empty;int h=Math.Max(36,Height*View/Total);int y=(int)((Height-h)*(Offset/(double)Math.Max(1,Total-View)));return new Rectangle(0,y,Width,h);}}
        protected override void OnPaint(PaintEventArgs e){var t=Thumb;if(t.IsEmpty)return;UiTheme.Fill(e.Graphics,UiTheme.Blend(UiTheme.Bg,Color.White,.04f),new RectangleF(0,0,Width-1,Height-1),Width/2f);UiTheme.Fill(e.Graphics,drag||Hover?UiTheme.Blend(UiTheme.Border,UiTheme.Accent,.6f):UiTheme.Border,new RectangleF(t.X,t.Y,t.Width-1,t.Height-1),Width/2f);}
        protected override void OnMouseDown(MouseEventArgs e){var t=Thumb;if(t.IsEmpty)return;if(!t.Contains(e.Location))ScrollTo(Offset+(e.Y<t.Y?-View:View));drag=true;grabY=e.Y;grabScroll=Offset;base.OnMouseDown(e);}
        protected override void OnMouseMove(MouseEventArgs e){if(drag){var t=Thumb;int track=Math.Max(1,Height-t.Height);ScrollTo(grabScroll+(e.Y-grabY)*(Total-View)/track);}base.OnMouseMove(e);}
        protected override void OnMouseUp(MouseEventArgs e){drag=false;Invalidate();base.OnMouseUp(e);}
        public void ScrollTo(int y){target.AutoScrollPosition=new Point(0,Math.Max(0,y));Invalidate();}
        public void ScrollBy(int dy){ScrollTo(Offset+dy);}
    }
    static class Program {
        [STAThread] static void Main(string[] args){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);if(args.Length==2&&args[0].StartsWith("--render")){using(var f=new MainForm())using(var b=new Bitmap(f.Width,f.Height)){f.Show();Application.DoEvents();if(args[0]=="--render-crystal")f.ShowCategoryForRender("Crystal");else if(args[0]=="--render-crystal-mid")f.ShowCategoryScrolledForRender("Crystal",430);else if(args[0]=="--render-crystal-low")f.ShowCategoryScrolledForRender("Crystal",880);else if(args[0]=="--render-crystal-bottom")f.ShowCategoryScrolledForRender("Crystal",1180);else if(args[0]=="--render-spear")f.ShowCategoryForRender("Spear Mace");else if(args[0]=="--render-mace")f.ShowCategoryForRender("Mace");else if(args[0]=="--render-themes")f.ShowCategoryForRender("Themes");else if(args[0]=="--render-cart")f.ShowCategoryForRender("Cart");else if(args[0]=="--render-smp")f.ShowCategoryForRender("SMP");else if(args[0]=="--render-hit")f.ShowModuleForRender("crystal");else if(args[0]=="--render-lunge")f.ShowModuleForRender("lunge");else if(args[0]=="--render-pearl")f.ShowModuleForRender("pearl");Application.DoEvents();f.DrawToBitmap(b,new Rectangle(0,0,f.Width,f.Height));b.Save(args[1],System.Drawing.Imaging.ImageFormat.Png);f.Close();}return;}if(args.Length==2&&args[0]=="--self-test"){PrecisionEngine.SelfTest(args[1]);return;}Application.Run(new MainForm());}
    }

    sealed class HotkeyBox:TextBox {
        public Keys Hotkey{get;private set;}
        public HotkeyBox(Keys key){Hotkey=key;ReadOnly=true;ShortcutsEnabled=false;Text=Label(key);TextAlign=HorizontalAlignment.Center;BackColor=UiTheme.Field;ForeColor=Color.FromArgb(235,238,242);BorderStyle=BorderStyle.FixedSingle;Font=new Font("Segoe UI",9F,FontStyle.Bold);Cursor=Cursors.Hand;}
        protected override void OnKeyDown(KeyEventArgs e){Hotkey=e.KeyCode;Text=Label(Hotkey);e.SuppressKeyPress=true;base.OnKeyDown(e);}
        protected override void OnMouseDown(MouseEventArgs e){Keys k=Keys.None;if(e.Button==MouseButtons.Middle)k=Keys.MButton;else if(e.Button==MouseButtons.XButton1)k=Keys.XButton1;else if(e.Button==MouseButtons.XButton2)k=Keys.XButton2;if(k!=Keys.None){Hotkey=k;Text=Label(k);}base.OnMouseDown(e);}
        public void SetKey(Keys key){Hotkey=key;Text=Label(key);}
        static string Label(Keys key){string s=key.ToString();if(s.StartsWith("D")&&s.Length==2&&Char.IsDigit(s[1]))return s.Substring(1);return s.ToUpperInvariant();}
    }

    sealed class ToggleSwitch:PaintedControl {
        bool on;public event EventHandler CheckedChanged;
        public bool Checked{get{return on;}set{if(on==value)return;on=value;Invalidate();if(CheckedChanged!=null)CheckedChanged(this,EventArgs.Empty);}}
        public ToggleSwitch(){Size=new Size(40,22);Cursor=Cursors.Hand;}
        protected override void OnClick(EventArgs e){if(Enabled)Checked=!Checked;base.OnClick(e);}
        protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;var r=new RectangleF(.5f,.5f,Width-1.5f,Height-1.5f);UiTheme.Fill(g,Checked?UiTheme.Accent:Color.FromArgb(44,52,62),r,r.Height/2);if(!Checked)UiTheme.Stroke(g,UiTheme.FieldBorder,r,r.Height/2);float d=Height-8,x=Checked?Width-d-4:4;using(var b=new SolidBrush(Checked?UiTheme.OnAccent:Color.FromArgb(150,161,173)))g.FillEllipse(b,x,4,d,d);}
    }

    sealed class MacroCard:Panel {
        public bool ActiveVisual;public int HeaderLine=78;
        public MacroCard(){DoubleBuffered=true;BackColor=Color.Transparent;SetStyle(ControlStyles.SupportsTransparentBackColor|ControlStyles.ResizeRedraw,true);}
        protected override void OnPaintBackground(PaintEventArgs e){var g=e.Graphics;g.Clear(UiTheme.Bg);var r=new RectangleF(.5f,.5f,Width-1.5f,Height-1.5f);UiTheme.Fill(g,UiTheme.Card,r,12);
            if(ActiveVisual&&HeaderLine>0){g.SetClip(new Rectangle(0,0,Width,HeaderLine));using(var p=UiTheme.Round(r,12))using(var glow=new LinearGradientBrush(new Rectangle(0,-1,Width,HeaderLine+2),Color.FromArgb(34,UiTheme.Accent),Color.FromArgb(0,UiTheme.Accent),90F))g.FillPath(glow,p);g.ResetClip();}
            UiTheme.Stroke(g,ActiveVisual?UiTheme.Blend(UiTheme.Border,UiTheme.Accent,.7f):UiTheme.Border,r,12);
            if(HeaderLine>0)using(var line=new Pen(UiTheme.Divider))g.DrawLine(line,22,HeaderLine,Width-22,HeaderLine);}
    }

    // Rounded input shell; hosts a borderless TextBox or a NumericUpDown with its native spinner swapped for -/+ buttons.
    sealed class RoundedField:Panel {
        readonly Control child;
        public RoundedField(Control child,int width=132){this.child=child;DoubleBuffered=true;BackColor=Color.Transparent;SetStyle(ControlStyles.SupportsTransparentBackColor|ControlStyles.ResizeRedraw,true);Size=new Size(width,30);var t=child as TextBox;if(t!=null)t.BorderStyle=BorderStyle.None;var n=child as NumericUpDown;
            if(n!=null){n.BorderStyle=BorderStyle.None;n.Controls[0].Visible=false;var minus=new StepButton(false,()=>n.DownButton());minus.SetBounds(2,2,26,Height-4);var plus=new StepButton(true,()=>n.UpButton());plus.SetBounds(Width-28,2,26,Height-4);Controls.Add(minus);Controls.Add(plus);}
            Controls.Add(child);}
        protected override void OnLayout(LayoutEventArgs e){base.OnLayout(e);bool num=child is NumericUpDown;int x=num?28:9,w=num?Width-40:Width-18;child.SetBounds(x,(Height-child.Height)/2+(num?1:0),w,child.Height);}
        protected override void OnEnter(EventArgs e){Invalidate();base.OnEnter(e);}
        protected override void OnLeave(EventArgs e){Invalidate();base.OnLeave(e);}
        protected override void OnPaint(PaintEventArgs e){var r=new RectangleF(.5f,.5f,Width-1.5f,Height-1.5f);UiTheme.Fill(e.Graphics,UiTheme.Field,r,7);UiTheme.Stroke(e.Graphics,ContainsFocus?UiTheme.Accent:UiTheme.FieldBorder,r,7);}
    }

    sealed class StepButton:PaintedControl {
        readonly bool plus;
        public StepButton(bool plus,Action click){this.plus=plus;Cursor=Cursors.Hand;Click+=delegate{click();};}
        protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;float cx=Width/2f,cy=Height/2f;using(var pen=new Pen(Hover?UiTheme.Accent:UiTheme.Subtle,1.6f)){g.DrawLine(pen,cx-4,cy,cx+4,cy);if(plus)g.DrawLine(pen,cx,cy-4,cx,cy+4);}}
    }

    sealed class ModuleState {
        public readonly string Id,Name,Description,Code;public readonly HotkeyBox Activate;public volatile bool Enabled;
        public ModuleState(string id,string code,string name,string description,Keys activate){Id=id;Code=code;Name=name;Description=description;Activate=new HotkeyBox(activate);}
    }

    sealed class Step {
        public readonly int Delay;public readonly Action Action;public readonly string Name;
        public Step(int delay,Action action,string name){Delay=delay;Action=action;Name=name;}
    }

    sealed class PrecisionEngine:IDisposable {
        readonly object gate=new object();readonly AutoResetEvent wake=new AutoResetEvent(false);readonly Thread worker;List<Step> pending;Action<string> progress;Action complete;volatile int generation;volatile bool busy,stopping;volatile string tag="";
        public bool Busy{get{return busy;}}public string Tag{get{return tag;}}
        public PrecisionEngine(){worker=new Thread(Loop){IsBackground=true,Name="Tonka Precision Input",Priority=ThreadPriority.AboveNormal};worker.Start();}
        public void Start(List<Step> steps,string name,Action<string> onProgress,Action onComplete){lock(gate){generation++;pending=new List<Step>(steps);progress=onProgress;complete=onComplete;tag=name;busy=true;}wake.Set();}
        public void Cancel(){lock(gate){generation++;pending=null;progress=null;complete=null;tag="";busy=false;}wake.Set();}
        void Loop(){while(!stopping){wake.WaitOne();if(stopping)return;List<Step> run;Action<string> report;Action done;int token;lock(gate){run=pending;report=progress;done=complete;token=generation;}if(run==null)continue;long target=Stopwatch.GetTimestamp();bool cancelled=false;foreach(var s in run){target+=(long)(s.Delay*(double)Stopwatch.Frequency/1000.0);while(true){if(stopping||token!=generation){cancelled=true;break;}long left=target-Stopwatch.GetTimestamp();if(left<=0)break;if(left>Stopwatch.Frequency/500)Thread.Sleep(1);else Thread.SpinWait(80);}if(cancelled)break;if(report!=null)report(s.Name);s.Action();}lock(gate){if(token==generation){busy=false;pending=null;tag="";}}if(!cancelled&&done!=null)done();}}
        public static void SelfTest(string path){var marks=new List<long>();using(var done=new ManualResetEvent(false))using(var e=new PrecisionEngine()){e.Start(new List<Step>{new Step(0,()=>marks.Add(Stopwatch.GetTimestamp()),"A"),new Step(5,()=>marks.Add(Stopwatch.GetTimestamp()),"B"),new Step(10,()=>marks.Add(Stopwatch.GetTimestamp()),"C")},"test",null,()=>done.Set());bool ok=done.WaitOne(2000)&&marks.Count==3;File.WriteAllText(path,(ok?"PASS":"FAIL")+Environment.NewLine+"Actions="+marks.Count+Environment.NewLine+"Clock="+Stopwatch.Frequency);}}
        public void Dispose(){stopping=true;Cancel();wake.Set();worker.Join(500);wake.Dispose();}
    }

    sealed class MainForm:Form,IMessageFilter {
        readonly Panel moduleList=new Panel(),viewport=new Panel(),topBar=new Panel();readonly Label pageTitle=new Label(),pageSub=new Label(),status=new Label();readonly StatusPill armedState=new StatusPill();readonly SoftButton arm=new SoftButton();readonly ScrollRail rail;readonly System.Windows.Forms.Timer uiTimer=new System.Windows.Forms.Timer();int railOffset;readonly System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer();readonly PrecisionEngine precision=new PrecisionEngine();readonly Dictionary<Keys,bool> old=new Dictionary<Keys,bool>();readonly Dictionary<string,NavItem> categoryButtons=new Dictionary<string,NavItem>();readonly Random random=new Random();readonly List<Step> queue=new List<Step>();
        readonly ModuleState anchor=new ModuleState("anchor","SA","Single Anchor","Place, charge and explode",Keys.G);
        readonly ModuleState crystal=new ModuleState("crystal","HC","Hit Crystal","Place obsidian once, then loop crystals",Keys.B);
        readonly ModuleState stun=new ModuleState("stun","SS","Stun Slam","Axe shield break into mace slam",Keys.R);
        readonly ModuleState pearl=new ModuleState("pearl","PC","Pearl Catch","Pearl throw into wind-charge catch",Keys.C);
        readonly ModuleState lunge=new ModuleState("lunge","LS","Lunge Swap","Carrier attribute swap into Lunge spear",Keys.V);
        readonly ModuleState cart=new ModuleState("cart","IC","Double Insta Cart","Rail, two TNT carts and bow shot",Keys.X);
        readonly ModuleState prebow=new ModuleState("prebow","PB","Pre-Bow Double Cart","Shoot first, flick down, then rail and carts",Keys.Z);
        readonly ModuleState shield=new ModuleState("shield","SH","Shield Stun","Axe double hit, then return to sword",Keys.H);
        readonly ModuleState dtap=new ModuleState("dtap","DT","Auto D-Tap","Sword damage-window into two crystal pops",Keys.T);
        readonly ModuleState safeAnchor=new ModuleState("safeAnchor","SF","Safe Anchor","Place, manual flick, charge and hold totem",Keys.Y);
        readonly ModuleState doubleAnchor=new ModuleState("doubleAnchor","DA","Double Anchor","Two complete anchor cycles with an air handoff",Keys.U);
        readonly ModuleState tripleAnchor=new ModuleState("tripleAnchor","TA","Triple Anchor","Three complete anchor cycles with air handoffs",Keys.I);
        readonly ModuleState obbyAir=new ModuleState("obbyAir","OA","Obby Air Place","Complete anchor cycle, then spam obsidian",Keys.O);
        readonly HotkeyBox anchorKey=new HotkeyBox(Keys.D4),glowKey=new HotkeyBox(Keys.D6),totemKey=new HotkeyBox(Keys.D5),obiKey=new HotkeyBox(Keys.D2),crystalKey=new HotkeyBox(Keys.D3);
        readonly HotkeyBox axeKey=new HotkeyBox(Keys.D1),maceKey=new HotkeyBox(Keys.D7),swordKey=new HotkeyBox(Keys.D2),pearlKey=new HotkeyBox(Keys.D6),windKey=new HotkeyBox(Keys.D2),lungeCarrier=new HotkeyBox(Keys.D2),spearKey=new HotkeyBox(Keys.D3),railKey=new HotkeyBox(Keys.D4),cartKey=new HotkeyBox(Keys.D5),cartKey2=new HotkeyBox(Keys.D8),bowKey=new HotkeyBox(Keys.D6);
        readonly HotkeyBox dtapSwordKey=new HotkeyBox(Keys.D1),dtapObiKey=new HotkeyBox(Keys.D2),dtapCrystalKey=new HotkeyBox(Keys.D3),safeAnchorKey=new HotkeyBox(Keys.D4),safeGlowKey=new HotkeyBox(Keys.D6),safeTotemKey=new HotkeyBox(Keys.D5),doubleAnchorKey=new HotkeyBox(Keys.D4),doubleGlowKey=new HotkeyBox(Keys.D6),doubleTotemKey=new HotkeyBox(Keys.D5),tripleAnchorKey=new HotkeyBox(Keys.D4),tripleGlowKey=new HotkeyBox(Keys.D6),tripleTotemKey=new HotkeyBox(Keys.D5),obbyAnchorKey=new HotkeyBox(Keys.D4),obbyGlowKey=new HotkeyBox(Keys.D6),obbyTotemKey=new HotkeyBox(Keys.D5),obbyObiKey=new HotkeyBox(Keys.D2);
        readonly HotkeyBox prebowBowKey=new HotkeyBox(Keys.D6),prebowRailKey=new HotkeyBox(Keys.D4),prebowCartKey=new HotkeyBox(Keys.D5),prebowCartKey2=new HotkeyBox(Keys.D8);
        readonly NumericUpDown anchorDelay=Num(25,180,60),crystalDelay=Num(0,500,28),crystalMs=Num(1,250,34);
        readonly NumericUpDown stunMaceFollow=Num(35,120,50),stunRandom=Num(0,15,5);
        readonly NumericUpDown pearlDelay=Num(0,1000,115),pearlSettle=Num(0,100,8),pearlRandom=Num(0,30,6);
        readonly NumericUpDown lungeSettle=Num(0,30,2),lungeGap=Num(0,49,3),lungeHold=Num(1,30,3),lungeReturn=Num(0,30,3),lungeRepeat=Num(40,1000,255);
        readonly NumericUpDown cartRailSettle=Num(20,300,55),cartPlaceGap=Num(20,300,55),cartAfterPlace=Num(20,300,40),cartBowSettle=Num(10,250,38),cartCharge=Num(50,1000,180),cartHold=Num(8,50,16),cartRandom=Num(0,20,2);
        readonly NumericUpDown prebowSettle=Num(20,180,55),prebowCharge=Num(80,1000,220),prebowFlick=Num(60,500,160),prebowRail=Num(25,250,55),prebowCart=Num(30,250,65),prebowGap=Num(5,100,12),prebowHold=Num(8,40,18);
        readonly NumericUpDown shieldDelay=Num(35,150,55),dtapSpeed=Num(35,120,55),dtapTick=Num(350,800,520),dtapBreak=Num(25,100,45);
        readonly NumericUpDown safeDelay=Num(25,250,60),safeFlick=Num(50,600,140),doubleDelay=Num(25,180,55),doubleGap=Num(5,60,18),doubleRandom=Num(0,10,2),tripleDelay=Num(25,180,55),tripleGap=Num(5,60,18),tripleRandom=Num(0,10,2),obbyDelay=Num(25,180,55),obbyAirDelay=Num(5,60,18),obbySpam=Num(10,100,24);
        readonly CheckBox lungeSpam=Option("Hold Activate to repeat",true);
        readonly string[] cleanFonts={"Segoe UI","Bahnschrift","Corbel","Candara","Trebuchet MS"};
        bool armed,dragging,crystalBase,applyingTheme;Point dragOrigin;long lungeDue;int selectedOpacity=100;string category="Crystal",selectedFont="Segoe UI",selectedAccent="#5CCDFC",selectedText="#F0F4F8";
        const int VK_END=0x23;

        [StructLayout(LayoutKind.Sequential)]struct INPUT{public uint type;public INPUTUNION data;}
        [StructLayout(LayoutKind.Explicit)]struct INPUTUNION{[FieldOffset(0)]public MOUSEINPUT mouse;[FieldOffset(0)]public KEYBDINPUT keyboard;}
        [StructLayout(LayoutKind.Sequential)]struct MOUSEINPUT{public int dx,dy;public uint mouseData,flags,time;public UIntPtr extra;}
        [StructLayout(LayoutKind.Sequential)]struct KEYBDINPUT{public ushort vk,scan;public uint flags,time;public UIntPtr extra;}
        [DllImport("user32.dll")]static extern short GetAsyncKeyState(int key);[DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();[DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);[DllImport("user32.dll",SetLastError=true)]static extern uint SendInput(uint count,INPUT[] input,int size);[DllImport("user32.dll")]static extern uint MapVirtualKey(uint code,uint type);[DllImport("winmm.dll")]static extern uint timeBeginPeriod(uint period);[DllImport("winmm.dll")]static extern uint timeEndPeriod(uint period);[DllImport("dwmapi.dll")]static extern int DwmSetWindowAttribute(IntPtr hwnd,int attribute,ref int value,int size);

        public MainForm(){Text="Tonka External";FormBorderStyle=FormBorderStyle.None;Size=new Size(1240,800);MinimumSize=MaximumSize=Size;BackColor=UiTheme.Bg;ForeColor=Color.White;Font=new Font("Segoe UI",9F);DoubleBuffered=true;rail=new ScrollRail(moduleList);BuildUi();LoadConfig();timeBeginPeriod(1);timer.Interval=5;timer.Tick+=Tick;timer.Start();uiTimer.Interval=50;uiTimer.Tick+=delegate{int y=-moduleList.AutoScrollPosition.Y;if(y!=railOffset){railOffset=y;rail.Invalidate();}};uiTimer.Start();Application.AddMessageFilter(this);FormClosing+=delegate{Application.RemoveMessageFilter(this);uiTimer.Stop();ReleaseButtons();SaveConfig();precision.Dispose();timeEndPeriod(1);};}
        protected override CreateParams CreateParams{get{var cp=base.CreateParams;cp.ClassStyle|=0x20000;return cp;}}
        protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);try{int round=2;DwmSetWindowAttribute(Handle,33,ref round,4);}catch{}}
        // Mouse wheel over the module area always scrolls the page instead of changing whichever number field is under the cursor.
        public bool PreFilterMessage(ref Message m){if(m.Msg!=0x20A||!Visible||!ContainsFocus&&Form.ActiveForm!=this)return false;var pt=viewport.PointToClient(Cursor.Position);if(!viewport.ClientRectangle.Contains(pt))return false;int delta=(short)(((long)m.WParam>>16)&0xFFFF);rail.ScrollBy(-delta*70/120);return true;}
        static NumericUpDown Num(int min,int max,int value){return new NumericUpDown{Minimum=min,Maximum=max,Value=value,BackColor=UiTheme.Field,ForeColor=Color.FromArgb(235,238,242),BorderStyle=BorderStyle.FixedSingle,TextAlign=HorizontalAlignment.Center,Font=new Font("Segoe UI",9F,FontStyle.Bold)};}
        static CheckBox Option(string text,bool value){var c=new CheckBox{Text=text,Checked=value,AutoSize=true,FlatStyle=FlatStyle.Flat,ForeColor=UiTheme.Muted,BackColor=Color.Transparent,Cursor=Cursors.Hand};c.FlatAppearance.BorderColor=UiTheme.FieldBorder;c.FlatAppearance.CheckedBackColor=UiTheme.Field;return c;}

        void BuildUi(){
            Text="Tonka Macros";Size=new Size(1240,800);MinimumSize=MaximumSize=Size;BackColor=UiTheme.Bg;
            topBar.Dock=DockStyle.Top;topBar.Height=56;topBar.BackColor=UiTheme.Surface;topBar.Paint+=PaintTopBar;Controls.Add(topBar);
            armedState.Text="DISARMED";armedState.Font=new Font("Segoe UI",7.5F,FontStyle.Bold);armedState.SetBounds(878,16,100,24);topBar.Controls.Add(armedState);
            arm.Text="ARM CLIENT";arm.Primary=true;arm.Font=new Font("Segoe UI",8.5F,FontStyle.Bold);arm.SetBounds(990,12,134,32);arm.Click+=delegate{ToggleArm();};topBar.Controls.Add(arm);
            var minimize=new WindowButton(false);minimize.SetBounds(1140,0,50,55);minimize.Click+=delegate{WindowState=FormWindowState.Minimized;};topBar.Controls.Add(minimize);
            var close=new WindowButton(true);close.SetBounds(1190,0,50,55);close.Click+=delegate{Close();};topBar.Controls.Add(close);
            topBar.MouseDown+=DragStart;topBar.MouseMove+=DragMove;topBar.MouseUp+=DragEnd;

            var side=new Panel{BackColor=UiTheme.Surface};side.SetBounds(0,56,260,714);side.Paint+=PaintSide;Controls.Add(side);
            SideLabel(side,"MACROS",24,22);AddCategory(side,"Crystal",44,"");AddCategory(side,"Mace",88,"");AddCategory(side,"Spear Mace",132,"");AddCategory(side,"Cart",176,"");AddCategory(side,"SMP",220,"");
            SideLabel(side,"SETTINGS",24,284);AddCategory(side,"Themes",306,"");
            SideLabel(side,"SYSTEM",24,372);Chip(side,"Windows input only",null,396,UiTheme.Good,150);Chip(side,"Panic stop","END",430,UiTheme.Warn,128);

            pageTitle.AutoSize=true;pageTitle.Location=new Point(282,76);pageTitle.Font=new Font("Segoe UI",18F,FontStyle.Bold);pageTitle.ForeColor=Color.FromArgb(242,245,247);Controls.Add(pageTitle);
            pageSub.AutoSize=true;pageSub.Location=new Point(286,112);pageSub.Font=new Font("Segoe UI",9F);pageSub.ForeColor=UiTheme.Muted;Controls.Add(pageSub);
            // The panel is wider than its viewport so the native scrollbar is clipped away; ScrollRail replaces it.
            viewport.SetBounds(286,150,916,592);viewport.BackColor=UiTheme.Bg;Controls.Add(viewport);
            moduleList.SetBounds(0,0,916+SystemInformation.VerticalScrollBarWidth+4,592);moduleList.BackColor=UiTheme.Bg;moduleList.AutoScroll=true;moduleList.AutoScrollMargin=new Size(0,20);viewport.Controls.Add(moduleList);
            rail.SetBounds(1214,154,6,584);Controls.Add(rail);
            var footer=new Panel{BackColor=UiTheme.Surface};footer.SetBounds(0,770,1240,30);footer.Paint+=delegate(object s,PaintEventArgs e){using(var p=new Pen(UiTheme.Divider))e.Graphics.DrawLine(p,0,0,1240,0);UiTheme.Fill(e.Graphics,armed?UiTheme.Accent:UiTheme.Good,new RectangleF(18,12,6,6),3);};Controls.Add(footer);
            status.Text="READY  ·  Enable modules individually, then arm the client";status.SetBounds(32,7,1150,17);status.Font=new Font("Segoe UI",8.25F);status.ForeColor=UiTheme.Muted;status.AutoEllipsis=true;footer.Controls.Add(status);
            ShowCategory("Crystal");
        }
        void AddCategory(Control p,string name,int y,string icon){var b=new NavItem(icon,name){Font=new Font("Segoe UI",9.5F)};b.SetBounds(12,y,236,40);b.Click+=delegate{ShowCategory(name);};p.Controls.Add(b);categoryButtons[name]=b;}
        void ShowCategory(string name){category=name;pageTitle.Text=name=="Themes"?"Theme":name+" Macros";pageSub.Text=name=="Cart"?"Standard and pre-bow external double-cart sequences":name=="Themes"?"Accent color, text color, window opacity and font":"Configure your external "+name.ToLowerInvariant()+" input macros";foreach(var pair in categoryButtons){pair.Value.Selected=pair.Key==name;pair.Value.Invalidate();}
            moduleList.SuspendLayout();moduleList.Controls.Clear();moduleList.AutoScrollPosition=Point.Empty;var cards=new List<MacroCard>();if(name=="Crystal"){cards.Add(BuildCrystalCard());cards.Add(BuildAnchorCard());cards.Add(BuildDTapCard());cards.Add(BuildSafeAnchorCard());cards.Add(BuildDoubleAnchorCard());cards.Add(BuildTripleAnchorCard());cards.Add(BuildObbyAirCard());}else if(name=="Mace"){cards.Add(BuildStunCard());cards.Add(BuildPearlCard());}else if(name=="Spear Mace"){cards.Add(BuildStunCard());cards.Add(BuildLungeCard());}else if(name=="Cart"){cards.Add(BuildCartCard());cards.Add(BuildPrebowCard());}else if(name=="SMP")cards.Add(BuildShieldCard());else if(name=="Themes")cards.AddRange(BuildThemeCards());
            int[] columnY={0,0};foreach(var card in cards){int col=columnY[1]<columnY[0]?1:0;card.Location=new Point(col*458,columnY[col]);columnY[col]+=card.Height+20;moduleList.Controls.Add(card);}moduleList.ResumeLayout();rail.Invalidate();}
        public void ShowCategoryForRender(string name){ShowCategory(name);}
        public void ShowCategoryScrolledForRender(string name,int y){ShowCategory(name);rail.ScrollTo(y);}
        public void ShowModuleForRender(string id){if(id=="crystal")ShowCategory("Crystal");else if(id=="lunge")ShowCategory("Spear Mace");else if(id=="pearl")ShowCategory("Mace");else if(id=="cart")ShowCategory("Cart");}
        MacroCard Card(ModuleState m,int height){var card=new MacroCard{Size=new Size(438,height),ActiveVisual=m.Enabled};card.Controls.Add(new Badge{Text=m.Code,Location=new Point(22,19),Size=new Size(40,40),Font=new Font(selectedFont,9.5F,FontStyle.Bold)});card.Controls.Add(new Label{Text=m.Name,AutoSize=true,Location=new Point(74,18),Font=new Font(selectedFont,11F,FontStyle.Bold),ForeColor=UiTheme.Text});card.Controls.Add(new Label{Text=m.Description,AutoSize=true,Location=new Point(75,42),Font=new Font(selectedFont,8.25F),ForeColor=UiTheme.Muted});var toggle=new ToggleSwitch{Checked=m.Enabled};toggle.Location=new Point(376,28);toggle.CheckedChanged+=delegate{m.Enabled=toggle.Checked;card.ActiveVisual=m.Enabled;card.Invalidate(true);};card.Controls.Add(toggle);return card;}
        int CardRow(MacroCard card,string label,Control control,int y){card.Controls.Add(new Label{Text=label,AutoSize=true,Location=new Point(21,y+7),Font=new Font(selectedFont,9F),ForeColor=UiTheme.Blend(UiTheme.Card,UiTheme.Text,.85f)});control.Font=new Font(selectedFont,9F,FontStyle.Bold);var field=new RoundedField(control);field.Location=new Point(284,y);card.Controls.Add(field);return y+36;}
        void CardFoot(MacroCard card,string text,int y){var strip=new FlowStrip(text,394,new Font(selectedFont,7.25F,FontStyle.Bold)){Location=new Point(22,y+8)};card.Controls.Add(strip);card.Height=strip.Bottom+22;}
        MacroCard Section(string title,string sub){var c=new MacroCard{Size=new Size(438,100)};c.Controls.Add(new Label{Text=title,AutoSize=true,Location=new Point(20,18),Font=new Font(selectedFont,11F,FontStyle.Bold),ForeColor=UiTheme.Text});c.Controls.Add(new Label{Text=sub,AutoSize=true,Location=new Point(21,42),Font=new Font(selectedFont,8.25F),ForeColor=UiTheme.Muted});return c;}
        List<MacroCard> BuildThemeCards(){
            var a=Section("Accent color","Highlights, toggles and the active tab");
            a.Controls.Add(new Swatch(UiTheme.Accent,false){Bounds=new Rectangle(22,96,44,44),Cursor=Cursors.Default});a.Controls.Add(ThemeLabel(ColorHex(UiTheme.Accent),76,96,14F,UiTheme.Accent,FontStyle.Bold));a.Controls.Add(ThemeLabel("Current accent",78,124,8.25F,UiTheme.Muted,FontStyle.Regular));
            a.Controls.Add(ThemeLabel("QUICK COLORS",22,162,7.5F,UiTheme.Subtle,FontStyle.Bold));Color[] quick={Color.FromArgb(83,194,240),Color.FromArgb(132,111,239),Color.FromArgb(51,210,178),Color.FromArgb(83,128,232),Color.FromArgb(166,77,230),Color.FromArgb(34,211,238),Color.FromArgb(244,181,41),Color.FromArgb(249,105,24),Color.FromArgb(251,91,111),Color.FromArgb(235,67,139),Color.FromArgb(226,229,235)};for(int i=0;i<quick.Length;i++)AddThemeSwatch(a,quick[i],22+i*36,184,true);
            a.Controls.Add(ThemeLabel("CUSTOM",22,232,7.5F,UiTheme.Subtle,FontStyle.Bold));a.Controls.Add(new RoundedField(HexBox(UiTheme.Accent,v=>selectedAccent=v),180){Location=new Point(22,254)});a.Controls.Add(PickerButton(214,254,UiTheme.Accent,v=>selectedAccent=v));a.Controls.Add(ThemeLabel("Type a hex code and press Enter",21,290,7.5F,UiTheme.Subtle,FontStyle.Regular));
            a.Controls.Add(ThemeLabel("PREVIEW",22,330,7.5F,UiTheme.Subtle,FontStyle.Bold));a.Controls.Add(new ToggleSwitch{Checked=true,Enabled=false,Location=new Point(22,357)});a.Controls.Add(new Badge{Text="AB",Location=new Point(76,352),Size=new Size(32,32),Font=new Font(selectedFont,8.5F,FontStyle.Bold)});a.Controls.Add(new FlowStrip("ANCHOR > CLICK > TOTEM",290,new Font(selectedFont,7.25F,FontStyle.Bold)){Location=new Point(124,357)});
            var b=Section("Appearance","Text color, window opacity and font");
            b.Controls.Add(ThemeLabel("TEXT COLOR",22,96,7.5F,UiTheme.Subtle,FontStyle.Bold));Color[] textQuick={Color.FromArgb(235,243,252),Color.FromArgb(197,210,229),Color.FromArgb(157,178,212),Color.FromArgb(255,211,158),Color.FromArgb(250,170,189),Color.FromArgb(180,237,221),Color.FromArgb(189,169,244),Color.FromArgb(125,216,238),Color.FromArgb(243,229,194),Color.FromArgb(132,147,177)};for(int i=0;i<textQuick.Length;i++)AddThemeSwatch(b,textQuick[i],22+i*36,118,false);
            b.Controls.Add(new RoundedField(HexBox(UiTheme.Text,v=>selectedText=v),180){Location=new Point(22,162)});b.Controls.Add(PickerButton(214,162,UiTheme.Text,v=>selectedText=v));
            b.Controls.Add(ThemeLabel("WINDOW OPACITY",22,214,7.5F,UiTheme.Subtle,FontStyle.Bold));var opacityValue=new Label{Text=selectedOpacity+"%",AutoSize=false,TextAlign=ContentAlignment.MiddleRight,Bounds=new Rectangle(316,210,100,20),Font=new Font(selectedFont,8.25F,FontStyle.Bold),ForeColor=UiTheme.Text,BackColor=Color.Transparent};b.Controls.Add(opacityValue);
            var opacity=new Slider{Minimum=65,Maximum=100,Value=selectedOpacity,Bounds=new Rectangle(14,234,410,22)};opacity.ValueChanged+=v=>{selectedOpacity=v;Opacity=v/100.0;opacityValue.Text=v+"%";};b.Controls.Add(opacity);
            b.Controls.Add(ThemeLabel("INTERFACE FONT",22,276,7.5F,UiTheme.Subtle,FontStyle.Bold));for(int i=0;i<cleanFonts.Length;i++){string font=cleanFonts[i];var f=new SoftButton{Text=font=="Trebuchet MS"?"Trebuchet":font,PreviewFont=font,Selected=selectedFont==font,Font=new Font(selectedFont,8.25F),Bounds=new Rectangle(22+i*80,298,74,30)};f.Click+=delegate{selectedFont=font;ApplyTheme();};b.Controls.Add(f);}
            var reset=new SoftButton{Text="Reset to defaults",Font=new Font(selectedFont,8.25F),Bounds=new Rectangle(22,352,140,30)};reset.Click+=delegate{selectedAccent="#5CCDFC";selectedText="#F0F4F8";selectedFont="Segoe UI";selectedOpacity=100;ApplyTheme();};b.Controls.Add(reset);
            a.Height=b.Height=406;return new List<MacroCard>{a,b};}
        TextBox HexBox(Color value,Action<string> apply){var t=new TextBox{Text=ColorHex(value),BackColor=UiTheme.Field,ForeColor=value,Font=new Font(selectedFont,9.5F,FontStyle.Bold)};t.KeyDown+=delegate(object s,KeyEventArgs e){if(e.KeyCode==Keys.Enter){Color parsed;if(TryHex(t.Text,out parsed)){apply(ColorHex(parsed));ApplyTheme();}e.SuppressKeyPress=true;}};return t;}
        SoftButton PickerButton(int x,int y,Color current,Action<string> apply){var b=new SoftButton{Text="Open color picker",Font=new Font(selectedFont,8.25F),Bounds=new Rectangle(x,y,202,30)};b.Click+=delegate{using(var d=new ColorDialog{Color=current,FullOpen=true})if(d.ShowDialog()==DialogResult.OK){apply(ColorHex(d.Color));ApplyTheme();}};return b;}
        Label ThemeLabel(string text,int x,int y,float size,Color color,FontStyle style){return new Label{Text=text,AutoSize=true,Location=new Point(x,y),Font=new Font(selectedFont,size,style),ForeColor=color,BackColor=Color.Transparent};}
        void AddThemeSwatch(Control parent,Color color,int x,int y,bool accent){bool selected=(accent?ColorHex(UiTheme.Accent):ColorHex(UiTheme.Text))==ColorHex(color);var s=new Swatch(color,selected){Bounds=new Rectangle(x,y,30,30)};s.Click+=delegate{if(accent)selectedAccent=ColorHex(color);else selectedText=ColorHex(color);ApplyTheme();};parent.Controls.Add(s);}
        string ColorHex(Color c){return "#"+c.R.ToString("X2")+c.G.ToString("X2")+c.B.ToString("X2");}
        bool TryHex(string value,out Color color){color=Color.White;try{string s=value.Trim().TrimStart('#');if(s.Length!=6)return false;color=Color.FromArgb(Convert.ToInt32(s.Substring(0,2),16),Convert.ToInt32(s.Substring(2,2),16),Convert.ToInt32(s.Substring(4,2),16));return true;}catch{return false;}}
        void ApplyTheme(){if(applyingTheme)return;applyingTheme=true;try{Color parsed;UiTheme.Accent=TryHex(selectedAccent,out parsed)?parsed:AccentByName(selectedAccent);UiTheme.Text=TryHex(selectedText,out parsed)?parsed:Color.FromArgb(240,244,248);Opacity=Math.Max(.65,Math.Min(1.0,selectedOpacity/100.0));ApplyFontTree(this);ShowCategory(category);Invalidate(true);}finally{applyingTheme=false;}}
        void ApplyFontTree(Control root){foreach(Control c in root.Controls){try{c.Font=new Font(selectedFont,c.Font.Size,c.Font.Style);}catch{}if(c.HasChildren)ApplyFontTree(c);}}
        Color AccentByName(string name){if(name=="Amethyst")return Color.FromArgb(155,123,255);if(name=="Emerald")return Color.FromArgb(66,214,164);if(name=="Crimson")return Color.FromArgb(255,100,124);if(name=="Gold")return Color.FromArgb(245,185,76);if(name=="Rose")return Color.FromArgb(240,120,200);return Color.FromArgb(92,205,252);}
        MacroCard BuildAnchorCard(){var c=Card(anchor,335);int y=96;y=CardRow(c,"Activate Key",anchor.Activate,y);y=CardRow(c,"Anchor Key",anchorKey,y);y=CardRow(c,"Glowstone Key",glowKey,y);y=CardRow(c,"Totem / Explode Key",totemKey,y);y=CardRow(c,"Delay (ms)",anchorDelay,y);CardFoot(c,"ANCHOR > CLICK > GLOWSTONE > CLICK > TOTEM > CLICK",y+4);return c;}
        MacroCard BuildCrystalCard(){var c=Card(crystal,370);int y=96;y=CardRow(c,"Activate Key (hold)",crystal.Activate,y);y=CardRow(c,"Obsidian Key",obiKey,y);y=CardRow(c,"Crystal Key",crystalKey,y);y=CardRow(c,"Initial Delay (ms)",crystalDelay,y);y=CardRow(c,"Crystal Speed (ms)",crystalMs,y);CardFoot(c,"HOLD > OBI ONCE > PLACE > BREAK > LOOP",y+4);return c;}
        MacroCard BuildStunCard(){var c=Card(stun,370);int y=96;y=CardRow(c,"Activate Key",stun.Activate,y);y=CardRow(c,"Axe Key",axeKey,y);y=CardRow(c,"Mace Key",maceKey,y);y=CardRow(c,"Mace Timing (ms)",stunMaceFollow,y);y=CardRow(c,"Randomize (%)",stunRandom,y);CardFoot(c,"ACTIVATE > AXE HIT > 10 MS SWITCH > MACE SLAM",y+4);return c;}
        MacroCard BuildPearlCard(){var c=Card(pearl,405);int y=96;y=CardRow(c,"Activate Key",pearl.Activate,y);y=CardRow(c,"Pearl Key",pearlKey,y);y=CardRow(c,"Wind Charge Key",windKey,y);y=CardRow(c,"Switch Settle (ms)",pearlSettle,y);y=CardRow(c,"Catch Delay (ms)",pearlDelay,y);y=CardRow(c,"Randomize (%)",pearlRandom,y);CardFoot(c,"PEARL > THROW > DELAY > WIND > USE",y+4);return c;}
        MacroCard BuildLungeCard(){var c=Card(lunge,485);int y=96;y=CardRow(c,"Activate Key",lunge.Activate,y);y=CardRow(c,"Carrier / Wind Key",lungeCarrier,y);y=CardRow(c,"Lunge Spear Key",spearKey,y);y=CardRow(c,"Carrier Settle (ms)",lungeSettle,y);y=CardRow(c,"Spear > Click (ms)",lungeGap,y);y=CardRow(c,"Click Hold (ms)",lungeHold,y);y=CardRow(c,"Return Gap (ms)",lungeReturn,y);y=CardRow(c,"Repeat Interval (ms)",lungeRepeat,y);lungeSpam.Location=new Point(20,y+6);c.Controls.Add(lungeSpam);CardFoot(c,"CARRIER > SPEAR > JAB > CARRIER",y+34);return c;}
        MacroCard BuildCartCard(){var c=Card(cart,560);int y=96;y=CardRow(c,"Activate Key",cart.Activate,y);y=CardRow(c,"Rail Key",railKey,y);y=CardRow(c,"TNT Cart Key 1",cartKey,y);y=CardRow(c,"TNT Cart Key 2 / Fallback",cartKey2,y);y=CardRow(c,"Flame Bow Key",bowKey,y);y=CardRow(c,"Rail Settle (ms)",cartRailSettle,y);y=CardRow(c,"Cart Settle (ms)",cartPlaceGap,y);y=CardRow(c,"After Place (ms)",cartAfterPlace,y);y=CardRow(c,"Bow Settle (ms)",cartBowSettle,y);y=CardRow(c,"Bow Charge (ms)",cartCharge,y);y=CardRow(c,"Input Hold (ms)",cartHold,y);y=CardRow(c,"Randomize (%)",cartRandom,y);CardFoot(c,"RAIL > CART 1 > CART 2 > FLAME BOW > RELEASE",y+2);return c;}
        MacroCard BuildPrebowCard(){var c=Card(prebow,565);int y=96;y=CardRow(c,"Activate Key",prebow.Activate,y);y=CardRow(c,"Flame Bow Key",prebowBowKey,y);y=CardRow(c,"Rail Key",prebowRailKey,y);y=CardRow(c,"TNT Cart Key 1",prebowCartKey,y);y=CardRow(c,"TNT Cart Key 2",prebowCartKey2,y);y=CardRow(c,"Bow Settle (ms)",prebowSettle,y);y=CardRow(c,"Bow Charge (ms)",prebowCharge,y);y=CardRow(c,"Manual Flick Window (ms)",prebowFlick,y);y=CardRow(c,"Rail Settle (ms)",prebowRail,y);y=CardRow(c,"Cart Settle (ms)",prebowCart,y);y=CardRow(c,"Next Item Gap (ms)",prebowGap,y);y=CardRow(c,"Input Hold (ms)",prebowHold,y);CardFoot(c,"SHOOT UP > FLICK DOWN > RAIL > CART 1 > CART 2",y+2);return c;}
        MacroCard BuildShieldCard(){var c=Card(shield,335);int y=96;y=CardRow(c,"Activate Key",shield.Activate,y);y=CardRow(c,"Axe Key",axeKey,y);y=CardRow(c,"Sword Return Key",swordKey,y);y=CardRow(c,"Delay (ms)",shieldDelay,y);CardFoot(c,"AXE > HIT > DELAY > HIT > SWORD",y+6);return c;}
        MacroCard BuildDTapCard(){var c=Card(dtap,440);int y=96;y=CardRow(c,"Activate Key",dtap.Activate,y);y=CardRow(c,"Sword Key",dtapSwordKey,y);y=CardRow(c,"Obsidian Key",dtapObiKey,y);y=CardRow(c,"Crystal Key",dtapCrystalKey,y);y=CardRow(c,"Action Speed (ms)",dtapSpeed,y);y=CardRow(c,"Second Crystal (ms)",dtapTick,y);y=CardRow(c,"Crystal Break Gap (ms)",dtapBreak,y);CardFoot(c,"SWORD HIT > OBI > POP 1 > DAMAGE WINDOW > POP 2",y+3);return c;}
        MacroCard BuildSafeAnchorCard(){var c=Card(safeAnchor,370);int y=96;y=CardRow(c,"Activate Key",safeAnchor.Activate,y);y=CardRow(c,"Anchor Key",safeAnchorKey,y);y=CardRow(c,"Glowstone Key",safeGlowKey,y);y=CardRow(c,"Totem Key",safeTotemKey,y);y=CardRow(c,"Input Delay (ms)",safeDelay,y);y=CardRow(c,"Manual Flick Window (ms)",safeFlick,y);CardFoot(c,"PLACE > MANUAL FLICK > CHARGE > TOTEM > YOU EXPLODE",y+3);return c;}
        MacroCard BuildDoubleAnchorCard(){var c=Card(doubleAnchor,440);int y=96;y=CardRow(c,"Activate Key",doubleAnchor.Activate,y);y=CardRow(c,"Anchor Key",doubleAnchorKey,y);y=CardRow(c,"Glowstone Key",doubleGlowKey,y);y=CardRow(c,"Totem / Explode Key",doubleTotemKey,y);y=CardRow(c,"Step Delay (ms)",doubleDelay,y);y=CardRow(c,"Air Handoff (ms)",doubleGap,y);y=CardRow(c,"Randomize (%)",doubleRandom,y);CardFoot(c,"2× ANCHOR > CLICK > GLOW > CLICK > TOTEM > CLICK",y+3);return c;}
        MacroCard BuildTripleAnchorCard(){var c=Card(tripleAnchor,440);int y=96;y=CardRow(c,"Activate Key",tripleAnchor.Activate,y);y=CardRow(c,"Anchor Key",tripleAnchorKey,y);y=CardRow(c,"Glowstone Key",tripleGlowKey,y);y=CardRow(c,"Totem / Explode Key",tripleTotemKey,y);y=CardRow(c,"Step Delay (ms)",tripleDelay,y);y=CardRow(c,"Air Handoff (ms)",tripleGap,y);y=CardRow(c,"Randomize (%)",tripleRandom,y);CardFoot(c,"3× ANCHOR > CLICK > GLOW > CLICK > TOTEM > CLICK",y+3);return c;}
        MacroCard BuildObbyAirCard(){var c=Card(obbyAir,475);int y=96;y=CardRow(c,"Activate Key",obbyAir.Activate,y);y=CardRow(c,"Anchor Key",obbyAnchorKey,y);y=CardRow(c,"Glowstone Key",obbyGlowKey,y);y=CardRow(c,"Totem / Explode Key",obbyTotemKey,y);y=CardRow(c,"Obsidian Key",obbyObiKey,y);y=CardRow(c,"Step Delay (ms)",obbyDelay,y);y=CardRow(c,"Air Handoff (ms)",obbyAirDelay,y);y=CardRow(c,"Obby Spam Gap (ms)",obbySpam,y);CardFoot(c,"FULL ANCHOR CYCLE > OBI AIR PLACE ×2",y+3);return c;}
        void SideLabel(Control p,string text,int x,int y){p.Controls.Add(new Label{Text=text,AutoSize=true,Location=new Point(x,y),Font=new Font("Segoe UI",7.5F,FontStyle.Bold),ForeColor=UiTheme.Subtle});}
        void Chip(Control p,string text,string key,int y,Color color,int width){p.Controls.Add(new StatusPill{Text=text,Key=key,DotColor=color,Font=new Font("Segoe UI",7.5F,FontStyle.Bold),Bounds=new Rectangle(20,y,width,26)});}
        void PaintTopBar(object sender,PaintEventArgs e){var g=e.Graphics;var family=topBar.Font.FontFamily;var logo=new Rectangle(18,14,28,28);UiTheme.Fill(g,UiTheme.Accent,logo,8);
            using(var lf=new Font(family,10F,FontStyle.Bold))UiTheme.Write(g,"T",lf,logo,UiTheme.OnAccent,TextFormatFlags.HorizontalCenter);
            using(var bf=new Font(family,11F,FontStyle.Bold))using(var sf=new Font(family,8.25F)){int w=TextRenderer.MeasureText(g,"TonkaMacros",bf,Size.Empty,TextFormatFlags.NoPadding).Width;UiTheme.Write(g,"TonkaMacros",bf,new Rectangle(56,0,w+4,56),UiTheme.Text,TextFormatFlags.Left);UiTheme.Write(g,"External Input Client",sf,new Rectangle(56+w+12,1,220,56),UiTheme.Subtle,TextFormatFlags.Left);}
            using(var p=new Pen(UiTheme.Divider))g.DrawLine(p,0,55,topBar.Width,55);}
        void PaintSide(object sender,PaintEventArgs e){var g=e.Graphics;var side=(Control)sender;using(var p=new Pen(UiTheme.Divider))g.DrawLine(p,side.Width-1,0,side.Width-1,side.Height);
            var tile=new RectangleF(12.5f,630.5f,235,63);UiTheme.Fill(g,UiTheme.Card,tile,10);UiTheme.Stroke(g,UiTheme.Border,tile,10);var badge=new Rectangle(24,642,40,40);UiTheme.Fill(g,UiTheme.AccentDark,badge,9);
            using(var bf=new Font(side.Font.FontFamily,9.5F,FontStyle.Bold)){UiTheme.Write(g,"TK",bf,badge,UiTheme.Accent,TextFormatFlags.HorizontalCenter);UiTheme.Write(g,"Tonka External",bf,new Rectangle(76,642,160,20),UiTheme.Text,TextFormatFlags.Left);}
            using(var sf=new Font(side.Font.FontFamily,8F)){UiTheme.Fill(g,UiTheme.Good,new RectangleF(77,668,6,6),3);UiTheme.Write(g,"Focus lock enabled",sf,new Rectangle(89,661,150,20),UiTheme.Good,TextFormatFlags.Left);}}
        void SetArmedUi(bool on){armedState.Text=on?"ARMED":"DISARMED";armedState.DotColor=on?UiTheme.Good:UiTheme.Subtle;armedState.Invalidate();arm.Text=on?"DISARM":"ARM CLIENT";arm.Primary=!on;arm.Danger=on;arm.Invalidate();if(status.Parent!=null)status.Parent.Invalidate();}

        void ToggleArm(){armed=!armed;precision.Cancel();ReleaseButtons();crystalBase=false;lungeDue=0;SetArmedUi(armed);status.Text=armed?"Aktiv · nur bei fokussiertem Minecraft":"Bereit · Module einzeln aktivieren, dann Client armen";}
        void Tick(object sender,EventArgs e){bool panic=Down(Keys.End);if(panic&&!Was(Keys.End)){armed=false;precision.Cancel();ReleaseButtons();crystalBase=false;lungeDue=0;SetArmedUi(false);status.Text="PANIC STOP";}Remember(Keys.End,panic);bool focused=MinecraftForeground();if(!armed||!focused){if(precision.Busy){precision.Cancel();ReleaseButtons();status.Text="Gestoppt · Minecraft nicht fokussiert";}TrackAll();return;}bool crystalHeld=Down(crystal.Activate.Hotkey),lungeHeld=Down(lunge.Activate.Hotkey);if(precision.Busy){if(precision.Tag=="crystal"&&!crystalHeld){precision.Cancel();crystalBase=false;status.Text="Hit Crystal gestoppt";}else if(precision.Tag=="lunge"&&lungeSpam.Checked&&!lungeHeld){precision.Cancel();lungeDue=0;status.Text="Lunge Swap gestoppt";}TrackAll();return;}
            bool anchorEdge=Edge(anchor.Activate.Hotkey),stunEdge=Edge(stun.Activate.Hotkey),pearlEdge=Edge(pearl.Activate.Hotkey),cartEdge=Edge(cart.Activate.Hotkey),prebowEdge=Edge(prebow.Activate.Hotkey),shieldEdge=Edge(shield.Activate.Hotkey),dtapEdge=Edge(dtap.Activate.Hotkey),safeEdge=Edge(safeAnchor.Activate.Hotkey),doubleEdge=Edge(doubleAnchor.Activate.Hotkey),tripleEdge=Edge(tripleAnchor.Activate.Hotkey),obbyEdge=Edge(obbyAir.Activate.Hotkey),crystalEdge=crystalHeld&&!Was(crystal.Activate.Hotkey),lungeEdge=lungeHeld&&!Was(lunge.Activate.Hotkey);long now=Stopwatch.GetTimestamp();
            bool lungeReady=lungeSpam.Checked?(lungeHeld&&(lungeEdge||now>=lungeDue)):lungeEdge;
            if(crystal.Enabled&&crystalHeld)StartCrystal();else if(lunge.Enabled&&lungeReady)StartLunge(now);else if(stun.Enabled&&stunEdge)StartStun();else if(shield.Enabled&&shieldEdge)StartShield();else if(prebow.Enabled&&prebowEdge)StartPrebow();else if(cart.Enabled&&cartEdge)StartCart();else if(dtap.Enabled&&dtapEdge)StartDTap();else if(safeAnchor.Enabled&&safeEdge)StartSafeAnchor();else if(doubleAnchor.Enabled&&doubleEdge)StartDoubleAnchor();else if(tripleAnchor.Enabled&&tripleEdge)StartTripleAnchor();else if(obbyAir.Enabled&&obbyEdge)StartObbyAir();else if(pearl.Enabled&&pearlEdge)StartPearl();else if(anchor.Enabled&&anchorEdge)StartAnchor();
            if(!crystalHeld)crystalBase=false;if(!lungeHeld)lungeDue=0;TrackAll();}
        void StartAnchor(){queue.Clear();int d=(int)anchorDelay.Value;const int hold=8;AddKey(0,anchorKey.Hotkey,"Anchor-Key");AddClick(d,true,"Anchor platzieren",hold);AddKey(d,glowKey.Hotkey,"Glowstone-Key");AddClick(d,true,"Anchor laden",hold);AddKey(d,totemKey.Hotkey,"Totem-Key");AddClick(d,true,"Anchor explodieren",hold);Run("anchor","Single Anchor · key/click sequence");}
        void StartCrystal(){queue.Clear();int initial=(int)crystalDelay.Value,cycle=(int)crystalMs.Value;if(!crystalBase){AddKey(0,obiKey.Hotkey,"Obsidian auswählen");AddClick(initial,true,"Obsidian platzieren",3);AddKey(initial,crystalKey.Hotkey,"Crystal auswählen");crystalBase=true;}AddClick(cycle,true,"Crystal platzieren",3);AddClick(cycle,false,"Crystal brechen",3);Run("crystal","Hit Crystal · HOLD");}
        void StartStun(){queue.Clear();int follow=Jitter((int)stunMaceFollow.Value,(int)stunRandom.Value);const int axeReady=55,switchGap=10,hold=8;AddKey(0,axeKey.Hotkey,"Axt auswählen");AddClick(axeReady,false,"Ein Shield-Break-Hit",hold);AddKey(switchGap,maceKey.Hotkey,"Mace auswählen");AddClick(Math.Max(25,follow-switchGap),false,"Ein Mace-Slam",hold);Run("stun","Stun Slam · Axe > Mace");}
        void StartPearl(){queue.Clear();int settle=Jitter((int)pearlSettle.Value,(int)pearlRandom.Value),delay=Jitter((int)pearlDelay.Value,(int)pearlRandom.Value);AddKey(0,pearlKey.Hotkey,"Pearl auswählen");AddClick(settle,true,"Pearl werfen",3);AddKey(delay,windKey.Hotkey,"Wind Charge auswählen");AddClick(settle,true,"Wind Charge benutzen",3);Run("pearl","Pearl Catch");}
        void StartLunge(long started){queue.Clear();int settle=(int)lungeSettle.Value,gap=(int)lungeGap.Value,hold=(int)lungeHold.Value,ret=(int)lungeReturn.Value;AddKey(0,lungeCarrier.Hotkey,"Carrier auswählen");AddKey(settle,spearKey.Hotkey,"Spear auswählen");AddClick(gap,false,"Lunge Jab",hold);AddKey(ret,lungeCarrier.Hotkey,"Carrier zurück");lungeDue=started+(long)((int)lungeRepeat.Value*(double)Stopwatch.Frequency/1000.0);Run("lunge","Lunge Swap · HOLD");}
        void StartCart(){queue.Clear();int variance=(int)cartRandom.Value,rail=Jitter((int)cartRailSettle.Value,variance),place1=Jitter((int)cartPlaceGap.Value,variance),place2=Jitter((int)cartPlaceGap.Value,variance),after=Jitter((int)cartAfterPlace.Value,variance),bow=Jitter((int)cartBowSettle.Value,variance),charge=Jitter((int)cartCharge.Value,variance),hold=(int)cartHold.Value;AddKey(0,railKey.Hotkey,"Schiene auswählen");AddClick(rail,true,"Schiene platzieren",hold);AddKey(after,cartKey.Hotkey,"TNT Cart 1 auswählen");AddClick(place1,true,"TNT Cart 1 platzieren",hold);AddKey(after,cartKey2.Hotkey,"TNT Cart 2 / Fallback auswählen");AddClick(place2,true,"TNT Cart 2 platzieren",hold);AddKey(after,bowKey.Hotkey,"Flame Bow auswählen");queue.Add(new Step(bow,()=>MouseState(true,true),"Bogen spannen"));queue.Add(new Step(charge,()=>MouseState(true,false),"Pfeil lösen"));Run("cart","Double Insta Cart");}
        void StartPrebow(){queue.Clear();int hold=(int)prebowHold.Value,next=(int)prebowGap.Value;AddKey(0,prebowBowKey.Hotkey,"Flame Bow auswählen");queue.Add(new Step((int)prebowSettle.Value,()=>MouseState(true,true),"Bogen spannen"));queue.Add(new Step((int)prebowCharge.Value,()=>MouseState(true,false),"Pfeil nach oben lösen"));AddKey((int)prebowFlick.Value,prebowRailKey.Hotkey,"Nach manuellem Flick: Schiene");AddClick((int)prebowRail.Value,true,"Schiene platzieren",hold);AddKey(next,prebowCartKey.Hotkey,"TNT Cart 1 auswählen");AddClick((int)prebowCart.Value,true,"TNT Cart 1 platzieren",hold);AddKey(next,prebowCartKey2.Hotkey,"TNT Cart 2 auswählen");AddClick((int)prebowCart.Value,true,"TNT Cart 2 platzieren",hold);Run("prebow","Pre-Bow Double Cart");}
        void StartShield(){queue.Clear();int d=(int)shieldDelay.Value;const int hold=12;AddKey(0,axeKey.Hotkey,"Axt auswählen");AddClick(d,false,"Shield Stun Hit 1",hold);AddClick(d,false,"Shield Stun Hit 2",hold);AddKey(d,swordKey.Hotkey,"Schwert zurück");Run("shield","Shield Stun");}
        void StartDTap(){queue.Clear();int speed=(int)dtapSpeed.Value,tick=(int)dtapTick.Value,breakGap=(int)dtapBreak.Value;const int hold=12,next=10;AddKey(0,dtapSwordKey.Hotkey,"Schwert auswählen");AddClick(speed,false,"Damage-Window mit Schwert starten",8);AddKey(next,dtapObiKey.Hotkey,"Obsidian auswählen");AddClick(speed,true,"Obsidian platzieren",hold);AddKey(next,dtapCrystalKey.Hotkey,"Crystal auswählen");AddClick(speed,true,"Crystal 1 platzieren",hold);AddClick(breakGap,false,"Crystal 1 im ersten Damage-Window brechen",8);AddClick(tick,true,"Crystal 2 nach Damage-Window platzieren",hold);AddClick(breakGap,false,"Crystal 2 brechen",8);Run("dtap","Auto D-Tap · 2 damage windows");}
        void StartSafeAnchor(){queue.Clear();int d=(int)safeDelay.Value;const int hold=20;AddKey(0,safeAnchorKey.Hotkey,"Anchor auswählen");AddClick(d,true,"Anchor platzieren",hold);AddKey((int)safeFlick.Value,safeGlowKey.Hotkey,"Nach manuellem Flick: Glowstone");AddClick(d,true,"Anchor laden",hold);AddKey(d,safeTotemKey.Hotkey,"Totem auswählen");Run("safeAnchor","Safe Anchor · manuell explodieren");}
        void StartDoubleAnchor(){queue.Clear();int d=(int)doubleDelay.Value,air=(int)doubleGap.Value,r=(int)doubleRandom.Value;const int hold=8;AddKey(0,doubleAnchorKey.Hotkey,"Anchor 1 Key");AddClick(Jitter(d,r),true,"Anchor 1 platzieren",hold);AddKey(Jitter(d,r),doubleGlowKey.Hotkey,"Glowstone 1 Key");AddClick(Jitter(d,r),true,"Anchor 1 laden",hold);AddKey(Jitter(d,r),doubleTotemKey.Hotkey,"Totem 1 Key");AddClick(Jitter(d,r),true,"Anchor 1 explodieren",hold);AddKey(0,doubleAnchorKey.Hotkey,"Anchor 2 Air-Handoff Key");AddClick(Jitter(air,r),true,"Anchor 2 air-placen",hold);AddKey(Jitter(d,r),doubleGlowKey.Hotkey,"Glowstone 2 Key");AddClick(Jitter(d,r),true,"Anchor 2 laden",hold);AddKey(Jitter(d,r),doubleTotemKey.Hotkey,"Totem 2 Key");AddClick(Jitter(d,r),true,"Anchor 2 explodieren",hold);Run("doubleAnchor","Double Anchor · simple sequence");}
        void StartTripleAnchor(){queue.Clear();int d=(int)tripleDelay.Value,air=(int)tripleGap.Value,r=(int)tripleRandom.Value;const int hold=8;AddKey(0,tripleAnchorKey.Hotkey,"Anchor 1 Key");AddClick(Jitter(d,r),true,"Anchor 1 platzieren",hold);AddKey(Jitter(d,r),tripleGlowKey.Hotkey,"Glowstone 1 Key");AddClick(Jitter(d,r),true,"Anchor 1 laden",hold);AddKey(Jitter(d,r),tripleTotemKey.Hotkey,"Totem 1 Key");AddClick(Jitter(d,r),true,"Anchor 1 explodieren",hold);for(int i=2;i<=3;i++){AddKey(0,tripleAnchorKey.Hotkey,"Anchor "+i+" Air-Handoff Key");AddClick(Jitter(air,r),true,"Anchor "+i+" air-placen",hold);AddKey(Jitter(d,r),tripleGlowKey.Hotkey,"Glowstone "+i+" Key");AddClick(Jitter(d,r),true,"Anchor "+i+" laden",hold);AddKey(Jitter(d,r),tripleTotemKey.Hotkey,"Totem "+i+" Key");AddClick(Jitter(d,r),true,"Anchor "+i+" explodieren",hold);}Run("tripleAnchor","Triple Anchor · simple sequence");}
        void StartObbyAir(){queue.Clear();int d=(int)obbyDelay.Value,air=(int)obbyAirDelay.Value,spam=(int)obbySpam.Value;const int hold=8;AddKey(0,obbyAnchorKey.Hotkey,"Anchor-Key");AddClick(d,true,"Anchor platzieren",hold);AddKey(d,obbyGlowKey.Hotkey,"Glowstone-Key");AddClick(d,true,"Anchor laden",hold);AddKey(d,obbyTotemKey.Hotkey,"Totem-Key");AddClick(d,true,"Anchor explodieren",hold);AddKey(0,obbyObiKey.Hotkey,"Obsidian Air-Handoff Key");AddClick(air,true,"Obsidian Air Place 1",hold);AddClick(spam,true,"Obsidian Air Place 2",hold);Run("obbyAir","Obby Air Place · full simple cycle");}
        int Jitter(int value,int percent){if(value<=0||percent<=0)return value;int spread=Math.Max(1,(int)Math.Round(value*percent/100.0));return Math.Max(0,value+random.Next(-spread,spread+1));}
        void AddKey(int delay,Keys key,string name){queue.Add(new Step(delay,()=>Tap(key),name));}void AddClick(int delay,bool right,string name,int hold){queue.Add(new Step(delay,()=>SendClick(right,hold),name));}
        void Run(string tag,string name){var steps=new List<Step>(queue);queue.Clear();status.Text=name;precision.Start(steps,tag,s=>Ui(()=>status.Text=name+" · "+s),()=>Ui(()=>status.Text="Bereit"));}
        void Ui(Action a){if(IsDisposed||!IsHandleCreated)return;try{BeginInvoke(a);}catch{}}
        void Tap(Keys key){if(IsMouseKey(key)){MouseVirtual(key,true);Pulse(2);MouseVirtual(key,false);return;}ushort scan=(ushort)MapVirtualKey((uint)key,0);var i=new INPUT[2];i[0].type=1;i[0].data.keyboard.scan=scan;i[0].data.keyboard.flags=8;i[1].type=1;i[1].data.keyboard.scan=scan;i[1].data.keyboard.flags=10;SendInput(2,i,Marshal.SizeOf(typeof(INPUT)));}
        void SendClick(bool right,int hold){MouseState(right,true);Pulse(hold);MouseState(right,false);}void MouseState(bool right,bool down){var i=new INPUT[1];i[0].type=0;i[0].data.mouse.flags=right?(down?8u:16u):(down?2u:4u);SendInput(1,i,Marshal.SizeOf(typeof(INPUT)));}void ReleaseButtons(){MouseState(false,false);MouseState(true,false);}void MouseVirtual(Keys key,bool down){var i=new INPUT[1];i[0].type=0;if(key==Keys.LButton)i[0].data.mouse.flags=down?2u:4u;else if(key==Keys.RButton)i[0].data.mouse.flags=down?8u:16u;else if(key==Keys.MButton)i[0].data.mouse.flags=down?32u:64u;else{i[0].data.mouse.flags=down?128u:256u;i[0].data.mouse.mouseData=key==Keys.XButton1?1u:2u;}SendInput(1,i,Marshal.SizeOf(typeof(INPUT)));}
        bool IsMouseKey(Keys k){return k==Keys.LButton||k==Keys.RButton||k==Keys.MButton||k==Keys.XButton1||k==Keys.XButton2;}void Pulse(int ms){ms=Math.Max(1,ms);long end=Stopwatch.GetTimestamp()+(long)(ms*(double)Stopwatch.Frequency/1000.0);if(ms>2)Thread.Sleep(ms-1);while(Stopwatch.GetTimestamp()<end)Thread.SpinWait(40);}
        bool Down(Keys k){return(GetAsyncKeyState((int)k)&0x8000)!=0;}bool Was(Keys k){bool b;return old.TryGetValue(k,out b)&&b;}bool Edge(Keys k){return Down(k)&&!Was(k);}void Remember(Keys k,bool v){old[k]=v;}
        void TrackAll(){foreach(var m in new[]{anchor,crystal,stun,pearl,lunge,cart,prebow,shield,dtap,safeAnchor,doubleAnchor,tripleAnchor,obbyAir})Remember(m.Activate.Hotkey,Down(m.Activate.Hotkey));if(!Down(crystal.Activate.Hotkey))crystalBase=false;if(!Down(lunge.Activate.Hotkey))lungeDue=0;}
        bool MinecraftForeground(){try{uint pid;GetWindowThreadProcessId(GetForegroundWindow(),out pid);if(pid==0)return false;using(var p=Process.GetProcessById((int)pid)){string n=p.ProcessName.ToLowerInvariant(),t=p.MainWindowTitle.ToLowerInvariant();return n=="java"||n=="javaw"||t.Contains("minecraft")||t.Contains("lunar")||t.Contains("badlion")||t.Contains("feather");}}catch{return false;}}

        string ConfigPath(){return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"tonka-external-client.ini");}
        void SaveConfig(){try{
            var x=new List<string>();Put(x,"themeFont",selectedFont);Put(x,"themeAccent",selectedAccent);Put(x,"themeText",selectedText);Put(x,"themeOpacity",selectedOpacity);foreach(var m in new[]{anchor,crystal,stun,pearl,lunge,cart,prebow,shield,dtap,safeAnchor,doubleAnchor,tripleAnchor,obbyAir}){Put(x,m.Id+"On",m.Enabled);Put(x,m.Id+"Activate",m.Activate.Hotkey);}
            Put(x,"anchorKey",anchorKey.Hotkey);Put(x,"glowKey",glowKey.Hotkey);Put(x,"totemKey",totemKey.Hotkey);Put(x,"anchorDelay",anchorDelay.Value);
            Put(x,"obiKey",obiKey.Hotkey);Put(x,"crystalKey",crystalKey.Hotkey);Put(x,"crystalDelay",crystalDelay.Value);Put(x,"crystalMs",crystalMs.Value);
            Put(x,"axeKey",axeKey.Hotkey);Put(x,"maceKey",maceKey.Hotkey);Put(x,"stunMaceFollow",stunMaceFollow.Value);Put(x,"stunRandom",stunRandom.Value);
            Put(x,"pearlKey",pearlKey.Hotkey);Put(x,"windKey",windKey.Hotkey);Put(x,"pearlDelay",pearlDelay.Value);Put(x,"pearlSettle",pearlSettle.Value);Put(x,"pearlRandom",pearlRandom.Value);
            Put(x,"lungeCarrier",lungeCarrier.Hotkey);Put(x,"spearKey",spearKey.Hotkey);Put(x,"lungeSettle",lungeSettle.Value);Put(x,"lungeGap",lungeGap.Value);Put(x,"lungeHold",lungeHold.Value);Put(x,"lungeReturn",lungeReturn.Value);Put(x,"lungeRepeat",lungeRepeat.Value);Put(x,"lungeSpam",lungeSpam.Checked);
            Put(x,"railKey",railKey.Hotkey);Put(x,"cartKey",cartKey.Hotkey);Put(x,"cartKey2",cartKey2.Hotkey);Put(x,"bowKey",bowKey.Hotkey);Put(x,"cartRailReady",cartRailSettle.Value);Put(x,"cartCartReady",cartPlaceGap.Value);Put(x,"cartAfterPlace",cartAfterPlace.Value);Put(x,"cartBowReady",cartBowSettle.Value);Put(x,"cartBowCharge",cartCharge.Value);Put(x,"cartInputHold",cartHold.Value);Put(x,"cartRandom",cartRandom.Value);
            Put(x,"prebowBowKey",prebowBowKey.Hotkey);Put(x,"prebowRailKey",prebowRailKey.Hotkey);Put(x,"prebowCartKey",prebowCartKey.Hotkey);Put(x,"prebowCartKey2",prebowCartKey2.Hotkey);Put(x,"prebowSettle",prebowSettle.Value);Put(x,"prebowCharge",prebowCharge.Value);Put(x,"prebowFlick",prebowFlick.Value);Put(x,"prebowRail",prebowRail.Value);Put(x,"prebowCart",prebowCart.Value);Put(x,"prebowGap",prebowGap.Value);Put(x,"prebowHold",prebowHold.Value);
            Put(x,"swordKey",swordKey.Hotkey);Put(x,"shieldDelay",shieldDelay.Value);
            Put(x,"dtapSwordKey",dtapSwordKey.Hotkey);Put(x,"dtapObiKey",dtapObiKey.Hotkey);Put(x,"dtapCrystalKey",dtapCrystalKey.Hotkey);Put(x,"dtapSpeed",dtapSpeed.Value);Put(x,"dtapTick",dtapTick.Value);Put(x,"dtapBreak",dtapBreak.Value);
            Put(x,"safeAnchorKey",safeAnchorKey.Hotkey);Put(x,"safeGlowKey",safeGlowKey.Hotkey);Put(x,"safeTotemKey",safeTotemKey.Hotkey);Put(x,"safeDelay",safeDelay.Value);Put(x,"safeFlick",safeFlick.Value);
            Put(x,"doubleAnchorKey",doubleAnchorKey.Hotkey);Put(x,"doubleGlowKey",doubleGlowKey.Hotkey);Put(x,"doubleTotemKey",doubleTotemKey.Hotkey);Put(x,"doubleDelay",doubleDelay.Value);Put(x,"doubleGap",doubleGap.Value);Put(x,"doubleRandom",doubleRandom.Value);Put(x,"tripleAnchorKey",tripleAnchorKey.Hotkey);Put(x,"tripleGlowKey",tripleGlowKey.Hotkey);Put(x,"tripleTotemKey",tripleTotemKey.Hotkey);Put(x,"tripleDelay",tripleDelay.Value);Put(x,"tripleGap",tripleGap.Value);Put(x,"tripleRandom",tripleRandom.Value);
            Put(x,"obbyAnchorKey",obbyAnchorKey.Hotkey);Put(x,"obbyGlowKey",obbyGlowKey.Hotkey);Put(x,"obbyTotemKey",obbyTotemKey.Hotkey);Put(x,"obbyObiKey",obbyObiKey.Hotkey);Put(x,"obbyDelay",obbyDelay.Value);Put(x,"obbyAir",obbyAirDelay.Value);Put(x,"obbySpam",obbySpam.Value);
            File.WriteAllLines(ConfigPath(),x.ToArray());
        }catch{}}
        void LoadConfig(){try{
            if(!File.Exists(ConfigPath()))return;var d=new Dictionary<string,string>();foreach(string line in File.ReadAllLines(ConfigPath())){int at=line.IndexOf('=');if(at>0)d[line.Substring(0,at)]=line.Substring(at+1);}
            string themeValue;if(d.TryGetValue("themeFont",out themeValue)&&Array.IndexOf(cleanFonts,themeValue)>=0)selectedFont=themeValue;if(d.TryGetValue("themeAccent",out themeValue))selectedAccent=themeValue;if(d.TryGetValue("themeText",out themeValue))selectedText=themeValue;int opacityValue;if(d.TryGetValue("themeOpacity",out themeValue)&&Int32.TryParse(themeValue,out opacityValue))selectedOpacity=Math.Max(65,Math.Min(100,opacityValue));Color themeColor;UiTheme.Accent=TryHex(selectedAccent,out themeColor)?themeColor:AccentByName(selectedAccent);UiTheme.Text=TryHex(selectedText,out themeColor)?themeColor:Color.FromArgb(240,244,248);
            foreach(var m in new[]{anchor,crystal,stun,pearl,lunge,cart,prebow,shield,dtap,safeAnchor,doubleAnchor,tripleAnchor,obbyAir}){ReadBool(d,m.Id+"On",v=>m.Enabled=v);ReadKey(d,m.Id+"Activate",m.Activate);}
            ReadKey(d,"anchorKey",anchorKey);ReadKey(d,"glowKey",glowKey);ReadKey(d,"totemKey",totemKey);ReadNum(d,"anchorDelay",anchorDelay);if(!d.ContainsKey("anchorDelay"))ReadNum(d,"anchorSettle",anchorDelay);
            ReadKey(d,"obiKey",obiKey);ReadKey(d,"crystalKey",crystalKey);ReadNum(d,"crystalDelay",crystalDelay);ReadNum(d,"crystalMs",crystalMs);
            ReadKey(d,"axeKey",axeKey);ReadKey(d,"maceKey",maceKey);ReadNum(d,"stunMaceFollow",stunMaceFollow);ReadNum(d,"stunRandom",stunRandom);
            ReadKey(d,"pearlKey",pearlKey);ReadKey(d,"windKey",windKey);ReadNum(d,"pearlDelay",pearlDelay);ReadNum(d,"pearlSettle",pearlSettle);ReadNum(d,"pearlRandom",pearlRandom);
            ReadKey(d,"lungeCarrier",lungeCarrier);ReadKey(d,"spearKey",spearKey);ReadNum(d,"lungeSettle",lungeSettle);ReadNum(d,"lungeGap",lungeGap);ReadNum(d,"lungeHold",lungeHold);ReadNum(d,"lungeReturn",lungeReturn);ReadNum(d,"lungeRepeat",lungeRepeat);bool b;if(d.ContainsKey("lungeSpam")&&Boolean.TryParse(d["lungeSpam"],out b))lungeSpam.Checked=b;
            ReadKey(d,"railKey",railKey);ReadKey(d,"cartKey",cartKey);ReadKey(d,"cartKey2",cartKey2);ReadKey(d,"bowKey",bowKey);ReadNum(d,"cartRailReady",cartRailSettle);ReadNum(d,"cartCartReady",cartPlaceGap);ReadNum(d,"cartAfterPlace",cartAfterPlace);ReadNum(d,"cartBowReady",cartBowSettle);ReadNum(d,"cartBowCharge",cartCharge);ReadNum(d,"cartInputHold",cartHold);ReadNum(d,"cartRandom",cartRandom);
            ReadKey(d,"prebowBowKey",prebowBowKey);ReadKey(d,"prebowRailKey",prebowRailKey);ReadKey(d,"prebowCartKey",prebowCartKey);ReadKey(d,"prebowCartKey2",prebowCartKey2);ReadNum(d,"prebowSettle",prebowSettle);ReadNum(d,"prebowCharge",prebowCharge);ReadNum(d,"prebowFlick",prebowFlick);ReadNum(d,"prebowRail",prebowRail);ReadNum(d,"prebowCart",prebowCart);ReadNum(d,"prebowGap",prebowGap);ReadNum(d,"prebowHold",prebowHold);
            ReadKey(d,"swordKey",swordKey);ReadNum(d,"shieldDelay",shieldDelay);
            ReadKey(d,"dtapSwordKey",dtapSwordKey);ReadKey(d,"dtapObiKey",dtapObiKey);ReadKey(d,"dtapCrystalKey",dtapCrystalKey);ReadNum(d,"dtapSpeed",dtapSpeed);ReadNum(d,"dtapTick",dtapTick);ReadNum(d,"dtapBreak",dtapBreak);
            ReadKey(d,"safeAnchorKey",safeAnchorKey);ReadKey(d,"safeGlowKey",safeGlowKey);ReadKey(d,"safeTotemKey",safeTotemKey);ReadNum(d,"safeDelay",safeDelay);ReadNum(d,"safeFlick",safeFlick);
            ReadKey(d,"doubleAnchorKey",doubleAnchorKey);ReadKey(d,"doubleGlowKey",doubleGlowKey);ReadKey(d,"doubleTotemKey",doubleTotemKey);ReadNum(d,"doubleDelay",doubleDelay);if(!d.ContainsKey("doubleDelay"))ReadNum(d,"doubleSettle",doubleDelay);ReadNum(d,"doubleGap",doubleGap);ReadNum(d,"doubleRandom",doubleRandom);ReadKey(d,"tripleAnchorKey",tripleAnchorKey);ReadKey(d,"tripleGlowKey",tripleGlowKey);ReadKey(d,"tripleTotemKey",tripleTotemKey);ReadNum(d,"tripleDelay",tripleDelay);if(!d.ContainsKey("tripleDelay"))ReadNum(d,"tripleSettle",tripleDelay);ReadNum(d,"tripleGap",tripleGap);ReadNum(d,"tripleRandom",tripleRandom);
            ReadKey(d,"obbyAnchorKey",obbyAnchorKey);ReadKey(d,"obbyGlowKey",obbyGlowKey);ReadKey(d,"obbyTotemKey",obbyTotemKey);ReadKey(d,"obbyObiKey",obbyObiKey);ReadNum(d,"obbyDelay",obbyDelay);if(!d.ContainsKey("obbyDelay"))ReadNum(d,"obbySettle",obbyDelay);ReadNum(d,"obbyAir",obbyAirDelay);ReadNum(d,"obbySpam",obbySpam);
            ApplyTheme();
        }catch{}}
        void Put(List<string>x,string k,object v){x.Add(k+"="+v);}void ReadBool(Dictionary<string,string>d,string k,Action<bool>a){string s;bool v;if(d.TryGetValue(k,out s)&&Boolean.TryParse(s,out v))a(v);}void ReadKey(Dictionary<string,string>d,string k,HotkeyBox h){string s;Keys key;int value;if(!d.TryGetValue(k,out s))return;if(Enum.TryParse<Keys>(s,true,out key))h.SetKey(key);else if(Int32.TryParse(s,out value))h.SetKey((Keys)value);}void ReadNum(Dictionary<string,string>d,string k,NumericUpDown n){string s;decimal v;if(d.TryGetValue(k,out s)&&Decimal.TryParse(s,out v))n.Value=Math.Max(n.Minimum,Math.Min(n.Maximum,v));}
        void DragStart(object s,MouseEventArgs e){if(e.Button==MouseButtons.Left){dragging=true;dragOrigin=e.Location;}}void DragMove(object s,MouseEventArgs e){if(dragging){Point at=((Control)s).PointToScreen(e.Location);Location=new Point(at.X-dragOrigin.X,at.Y-dragOrigin.Y);}}void DragEnd(object s,MouseEventArgs e){dragging=false;}
    }
}
