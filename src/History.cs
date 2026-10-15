using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
namespace KongeuniEditor
{
    public sealed class EditHistory : IDisposable
    {
        readonly Card model;
        readonly List<Card> undo=new List<Card>(), redo=new List<Card>();
        readonly HashSet<Bitmap> images=new HashSet<Bitmap>();
        string lastGroup; int lastTick;
        public event Action Changed;
        public bool CanUndo { get { return undo.Count>0; } }
        public bool CanRedo { get { return redo.Count>0; } }
        public EditHistory(Card card) { model=card;images.Add(card.Photo); }
        public void Change(Action action,string group) { Card before=model.Snapshot();action();Record(before,group); }
        public void Record(Card before,string group)
        {
            if(model.SameAs(before))return;
            bool coalesce=group!=null&&group==lastGroup&&Environment.TickCount-lastTick<650&&undo.Count>0&&redo.Count==0;
            if(!coalesce)undo.Add(before);
            images.Add(before.Photo);images.Add(model.Photo);redo.Clear();
            if(undo.Count>60)undo.RemoveAt(0);
            lastGroup=group;lastTick=Environment.TickCount;Collect();Notify();
        }
        public void Undo() { if(!CanUndo)return;redo.Add(model.Snapshot());Card previous=undo[undo.Count-1];undo.RemoveAt(undo.Count-1);model.Restore(previous);lastGroup=null;Notify(); }
        public void Redo() { if(!CanRedo)return;undo.Add(model.Snapshot());Card next=redo[redo.Count-1];redo.RemoveAt(redo.Count-1);model.Restore(next);lastGroup=null;Notify(); }
        void Notify() { if(Changed!=null)Changed(); }
        void Collect() {
            HashSet<Bitmap> used=new HashSet<Bitmap>();used.Add(model.Photo);
            foreach(Card s in undo)used.Add(s.Photo);foreach(Card s in redo)used.Add(s.Photo);
            foreach(Bitmap image in new List<Bitmap>(images))if(!used.Contains(image)){image.Dispose();images.Remove(image);}
        }
        public void Dispose() { undo.Clear();redo.Clear();Collect();images.Clear(); }
    }

    sealed class SoftButton : Button
    {
        bool hover;
        public bool Primary;
        public SoftButton() {
            SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);
            FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=Cursors.Hand;UseVisualStyleBackColor=false;
        }
        protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
        protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
        protected override void OnPaint(PaintEventArgs e) {
            e.Graphics.Clear(Parent==null?Color.White:Parent.BackColor);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            Color bg=Primary?Color.FromArgb(52,52,55):Color.FromArgb(239,239,241);
            if(hover&&Enabled)bg=Primary?Color.FromArgb(72,72,76):Color.FromArgb(228,228,231);
            if(!Enabled)bg=Color.FromArgb(244,244,245);
            using(Brush b=new SolidBrush(bg))Card.Round(e.Graphics,b,new RectangleF(0,0,Width-1,Height-1),11);
            TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,!Enabled?Color.FromArgb(175,175,179):Primary?Color.White:Color.FromArgb(63,63,68),TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
            if(Focused)ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(5,5,Width-11,Height-11));
        }
    }
}
