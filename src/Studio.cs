using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;
namespace KongeuniEditor
{
    public sealed class Studio : Form
    {
        public readonly Card Model=new Card();
        public readonly EditHistory History;
        internal readonly Preview PreviewCanvas;
        readonly Preview preview;
        TabControl inspectorTabs;
        ColorPicker activeColor;
        Panel inspectorHost,headerHost;
        Icon appIcon;
        readonly List<Action> bindings=new List<Action>();
        readonly ToolTip tips=new ToolTip{InitialDelay=380,ReshowDelay=150,AutoPopDelay=6000,ShowAlways=true};
        readonly Bubble empty=new Bubble();
        readonly Label status;
        readonly SoftButton undo,redo;
        readonly ComboBox bubbleList;
        readonly FlowLayoutPanel bubbleFields;
        readonly Font uiFont;
        bool syncing;
        string selectedId;
        Bubble Selected{get{return Model.Find(selectedId)??empty;}}
        readonly Color surface=Color.FromArgb(250,250,252),muted=Color.FromArgb(115,115,124);
        public Studio(){
            uiFont=Card.MakeFont(Card.FixedFont,11.25f,FontStyle.Regular,GraphicsUnit.Point);Font=uiFont;BackColor=surface;ForeColor=Color.FromArgb(48,48,54);
            Text="콩으니 카드뉴스 생성 에디터";StartPosition=FormStartPosition.CenterScreen;Size=new Size(1500,960);MinimumSize=new Size(1240,780);AutoScaleMode=AutoScaleMode.Dpi;
            History=new EditHistory(Model);
            using(Stream stream=System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("appicon"))if(stream!=null){using(Icon icon=new Icon(stream))appIcon=(Icon)icon.Clone();Icon=appIcon;}
            Panel header=new Panel{Dock=DockStyle.Top,Height=83,BackColor=surface};headerHost=header;
            header.Controls.Add(new Label{Text="콩으니 카드뉴스 생성 에디터",Font=Card.MakeFont(Card.FixedFont,17.25f,FontStyle.Regular,GraphicsUnit.Point),AutoSize=true,Location=new Point(26,26)});
            undo=Button("실행 취소",null);redo=Button("다시 실행",null);SoftButton save=Button("PNG 저장",null,true);undo.Width=115;redo.Width=115;save.Width=142;header.Controls.Add(undo);header.Controls.Add(redo);header.Controls.Add(save);
            header.Resize+=delegate{save.Location=new Point(header.Width-168,23);redo.Location=new Point(header.Width-295,23);undo.Location=new Point(header.Width-422,23);};
            undo.Click+=delegate{History.Undo();};redo.Click+=delegate{History.Redo();};save.Click+=delegate{Export();};
            status=new Label{Dock=DockStyle.Bottom,Height=36,Padding=new Padding(18,7,0,0),ForeColor=muted,BackColor=Color.FromArgb(244,244,247),Text="New Name · 캡처를 불러와 시작하세요."};
            TableLayoutPanel body=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Padding=new Padding(15,0,15,12)};
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,360));body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));body.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            Panel left=new Panel{Dock=DockStyle.Fill,Padding=new Padding(2,0,6,0)};inspectorHost=left;
            TabControl tabs=new TabControl{Dock=DockStyle.Fill,SizeMode=TabSizeMode.Fixed,ItemSize=new Size(64,30)};inspectorTabs=tabs;left.Controls.Add(tabs);
            FlowLayoutPanel title=Page(tabs,"제목"),background=Page(tabs,"배경"),photo=Page(tabs,"캡처"),brand=Page(tabs,"서명"),right=Page(tabs,"말풍선");
            Panel format=new Panel{Dock=DockStyle.Bottom,Height=87,Padding=new Padding(8,5,8,0)};
            Label formatLabel=new Label{Dock=DockStyle.Top,Height=30,Text="출력 규격"};format.Controls.Add(formatLabel);
            ComboBox formats=new QuietComboBox{Dock=DockStyle.Bottom,DropDownStyle=ComboBoxStyle.DropDownList,FlatStyle=FlatStyle.Flat,Margin=new Padding(0,0,0,6)};formats.Items.AddRange(new string[]{"세로 4:5 · 1080 × 1350","정사각 · 1080 × 1080","스토리 · 1080 × 1920"});format.Controls.Add(formats);left.Controls.Add(format);
            bindings.Add(delegate{formats.SelectedIndex=Model.Height==1350?0:Model.Height==1080?1:2;});formats.SelectedIndexChanged+=delegate{if(formats.SelectedIndex>=0)Edit(delegate{Model.Height=formats.SelectedIndex==0?1350:formats.SelectedIndex==1?1080:1920;Model.PhotoX=Model.PhotoY=0;foreach(Bubble b in Model.Bubbles)b.Y=Math.Min(b.Y,Model.Height-60);},null);};
            Panel middle=new Panel{Dock=DockStyle.Fill,Padding=new Padding(12,0,12,0)};
            preview=new Preview(Model){Dock=DockStyle.Fill};PreviewCanvas=preview;preview.ElementSelected=delegate(string element){int tab=element=="제목"?0:element=="캡처"?2:(element=="서명"||element=="플랫폼")?3:element=="말풍선"?4:1;tabs.SelectedIndex=tab;};preview.SelectionChanged=Select;preview.Committed=delegate(Card before){History.Record(before,null);};
            Label hint=new Label{Dock=DockStyle.Bottom,Height=56,Text="요소를 클릭하면 왼쪽 설정이 열려요 · 드래그로 이동\nShift + 말풍선 조절점: 비율과 글자 함께 확대 · 제목 크기는 고정",TextAlign=ContentAlignment.MiddleCenter,ForeColor=muted};middle.Controls.Add(preview);middle.Controls.Add(hint);
            body.Controls.Add(left,0,0);body.Controls.Add(middle,1,0);Controls.Add(body);Controls.Add(header);Controls.Add(status);

            Section(title,"제목 문구");
            Choice(title,new string[]{"제목 위 · 캡처 아래","캡처 위 · 제목 아래"},delegate{return Model.TitleAtBottom?1:0;},delegate(int i){Model.TitleAtBottom=i==1;Model.PhotoX=Model.PhotoY=Model.TitleX=Model.TitleY=0;});
            Input(title,delegate{return Model.Name;},delegate(string s){Model.Name=s;},"title",false);
            Note(title,"아래에 ‘카톡테마 공유’가 붙어요.");
            Button("색감으로 이름 추천",title).Click+=delegate{Edit(delegate{Model.Name=Card.SuggestName(Model.Accent);},null);};
            Note(title,"제목 글씨체");FontChoice(title,delegate{return Model.CardFontName;},delegate(string s){Model.CardFontName=s;});
            PaintButton(title,"제목 색",delegate{return Model.TitleFill;},delegate(PaintSpec p){Model.TitleFill=p;});
            Note(title,"제목 글자 크기는 고정이에요.");
            TextStyle(title,delegate{return Model.TitleStyle;});
            Section(title,"제목 위치");
            Number(title,"가로 이동",-500,500,delegate{return (int)Model.TitleX;},delegate(int v){Model.TitleX=v;},"title-x");
            Number(title,"세로 이동",-100,800,delegate{return (int)Model.TitleY;},delegate(int v){Model.TitleY=v;},"title-y");
            Note(title,"크기가 커지면 캔버스 밖으로 넘칠 수 있어요.");

            Section(background,"배경");
            PaintButton(background,"배경색",delegate{return Model.BackgroundFill;},delegate(PaintSpec p){Model.BackgroundFill=p;});
            Button("캡처에서 색 다시 추출",background).Click+=delegate{Edit(delegate{Model.ExtractPalette();},null);};
            Section(background,"배경 패턴");
            Choice(background,new string[]{"단색 배경","격자","엇갈린 도트","엇갈린 별","엇갈린 하트"},delegate{return Model.Pattern;},delegate(int i){Model.Pattern=i;if(i>=3&&Model.DotSize<16)Model.DotSize=24;});
            PaintButton(background,"패턴 색",delegate{return Model.PatternFill;},delegate(PaintSpec p){Model.PatternFill=p;});
            Number(background,"격자 크기 / 패턴 가로 간격",20,200,delegate{return Model.PatternSize;},delegate(int v){Model.PatternSize=v;}, "pattern-size");
            Number(background,"패턴 세로 간격 (%)",100,260,delegate{return Model.DotRowSpacing;},delegate(int v){Model.DotRowSpacing=v;}, "dot-row-spacing");
            Number(background,"도트 · 별 · 하트 크기",2,60,delegate{return Model.DotSize;},delegate(int v){Model.DotSize=v;}, "dot-size");
            Number(background,"패턴 진하기 (%)",5,100,delegate{return Model.PatternOpacity;},delegate(int v){Model.PatternOpacity=v;}, "pattern-opacity");

            Section(brand,"콩으니 카톡테마");
            Check(brand,"AND도 지원 (IOS/AND)",delegate{return Model.SupportsAndroid;},delegate(bool v){Model.SupportsAndroid=v;});
            Note(brand,"오른쪽 위 기본 표시: IOS");
            Note(brand,"서체는 갈무리11 레귤러로 유지해요.");
            PaintButton(brand,"문구 색",delegate{return Model.BrandFill;},delegate(PaintSpec p){Model.BrandFill=p;});
            TextStyle(brand,delegate{return Model.BrandStyle;});
            Number(brand,"문구 크기 (%)",40,400,delegate{return (int)(Model.BrandScale*100+.5f);},delegate(int v){Model.BrandScale=v/100f;},"brand-scale");

            Section(photo,"내 카톡테마 캡처");
            Button("＋ 캡처 불러오기",photo,true).Click+=delegate{OpenPhoto();};Button("클립보드에서 붙여넣기",photo).Click+=delegate{Paste();};Note(photo,"PNG · JPG · 파일 드롭도 가능해요.");
            Number(photo,"캡처 크기 (%)",40,160,delegate{return (int)(Model.PhotoScale*100+.5f);},delegate(int v){Model.PhotoScale=v/100f;},"photo-scale");
            Number(photo,"가로 이동",-600,600,delegate{return (int)Model.PhotoX;},delegate(int v){Model.PhotoX=v;},"photo-x");
            Number(photo,"세로 이동",-1500,1500,delegate{return (int)Model.PhotoY;},delegate(int v){Model.PhotoY=v;},"photo-y");
            Button("가로 가운데 정렬",photo).Click+=delegate{Edit(delegate{Model.PhotoX=0;},null);};
            Button("위치 · 크기 초기화",photo).Click+=delegate{Edit(delegate{Model.PhotoX=Model.PhotoY=0;Model.PhotoScale=1;},null);};
            Section(photo,"캡처 테두리");Check(photo,"테두리 사용",delegate{return Model.Frame;},delegate(bool v){Model.Frame=v;});
            SolidButton(photo,"캡처 테두리색",delegate{return Model.FrameColor;},delegate(Color c){Model.FrameColor=c;});
            Number(photo,"테두리 두께",0,50,delegate{return Model.FrameWidth;},delegate(int v){Model.FrameWidth=v;},"frame-width");
            Check(photo,"캡처 그림자",delegate{return Model.PhotoShadow;},delegate(bool v){Model.PhotoShadow=v;});
            Note(photo,"원본 캡처 속 글씨와 비율은 유지돼요.");

            Section(right,"내가 꾸미는 말풍선");Button("＋ 말풍선 추가",right,true).Click+=delegate{AddBubble();};
            bubbleList=new QuietComboBox{Width=296,DropDownStyle=ComboBoxStyle.DropDownList,FlatStyle=FlatStyle.Flat,Margin=new Padding(0,6,0,8)};right.Controls.Add(bubbleList);bubbleList.SelectedIndexChanged+=delegate{if(!syncing&&bubbleList.SelectedItem!=null)Select(((BubbleItem)bubbleList.SelectedItem).Id);};
            FlowLayoutPanel actions=new FlowLayoutPanel{Width=296,Height=88,Margin=new Padding(0,0,0,5)};
            SoftButton duplicate=Button("복제",null),delete=Button("삭제",null),front=Button("맨 앞으로",null),back=Button("맨 뒤로",null);foreach(SoftButton b in new SoftButton[]{duplicate,delete,front,back}){b.Width=142;b.Height=36;b.Margin=new Padding(0,0,6,6);actions.Controls.Add(b);}right.Controls.Add(actions);
            duplicate.Click+=delegate{DuplicateBubble();};delete.Click+=delegate{DeleteBubble();};
            front.Click+=delegate{Reorder(true);};back.Click+=delegate{Reorder(false);};bindings.Add(delegate{actions.Enabled=Model.Find(selectedId)!=null;});
            bubbleFields=new FlowLayoutPanel{Width=318,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,FlowDirection=FlowDirection.TopDown,WrapContents=false,Margin=new Padding(0),Padding=new Padding(0)};right.Controls.Add(bubbleFields);
            Note(bubbleFields,"문구 · Enter로 줄바꿈");Input(bubbleFields,delegate{return Selected.Text;},delegate(string s){Selected.Text=s;},"bubble-text",true);
            Note(bubbleFields,"말풍선 글씨체");FontChoice(bubbleFields,delegate{return Selected.FontName;},delegate(string s){Selected.FontName=s;});
            Note(bubbleFields,"모양");Choice(bubbleFields,new string[]{"둥근 말풍선","타원 말풍선","생각 구름","외침 말풍선","리본 말풍선","네모","동그라미"},delegate{return Selected.Kind;},delegate(int i){Selected.Kind=i;if(i==2&&Selected.Height<Selected.Width*.45f)Selected.Height=Selected.Width*.60f;if(i==6){float side=Math.Max(180,Math.Min(Selected.Width,320));Selected.Width=Selected.Height=side;}});
            Check(bubbleFields,"모양 좌우 반전 (글자 유지)",delegate{return Selected.Mirrored;},delegate(bool v){Selected.Mirrored=v;});
            Note(bubbleFields,"글자가 넘치면 말풍선 크기를 늘려주세요.");
            Number(bubbleFields,"글자 크기 (px)",6,220,delegate{return Selected.FontSize;},delegate(int v){Selected.FontSize=v;},"bubble-font");
            Number(bubbleFields,"가로 크기",150,1000,delegate{return (int)Selected.Width;},delegate(int v){Selected.Width=v;},"bubble-width");
            Number(bubbleFields,"세로 크기",75,650,delegate{return (int)Selected.Height;},delegate(int v){Selected.Height=v;},"bubble-height");
            Number(bubbleFields,"가로 위치",0,1080,delegate{return (int)Selected.X;},delegate(int v){Selected.X=v;},"bubble-x");
            Number(bubbleFields,"세로 위치",0,1920,delegate{return (int)Selected.Y;},delegate(int v){Selected.Y=v;},"bubble-y");
            Number(bubbleFields,"기울기 (°)",-180,180,delegate{return Selected.Rotation;},delegate(int v){Selected.Rotation=v;},"bubble-angle");
            SolidButton(bubbleFields,"말풍선 바탕색",delegate{return Selected.Fill;},delegate(Color c){Selected.Fill=c;});SolidButton(bubbleFields,"문구 색",delegate{return Selected.Ink;},delegate(Color c){Selected.Ink=c;});SolidButton(bubbleFields,"말풍선 선 색",delegate{return Selected.Line;},delegate(Color c){Selected.Line=c;});
            Number(bubbleFields,"말풍선 선 두께",0,12,delegate{return Selected.Border;},delegate(int v){Selected.Border=v;},"bubble-border");Check(bubbleFields,"말풍선 도형 그림자",delegate{return Selected.Shadow;},delegate(bool v){Selected.Shadow=v;});
            Section(bubbleFields,"말풍선 글자 효과");TextStyle(bubbleFields,delegate{return Selected.Letters;});
            AllowDrop=true;DragEnter+=delegate(object s,DragEventArgs e){if(e.Data.GetDataPresent(DataFormats.FileDrop))e.Effect=DragDropEffects.Copy;};DragDrop+=delegate(object s,DragEventArgs e){string[] paths=(string[])e.Data.GetData(DataFormats.FileDrop);if(paths.Length>0)LoadPhoto(paths[0]);};
            History.Changed+=Sync;FormClosing+=delegate{if(activeColor!=null)activeColor.Close();};FormClosed+=delegate{History.Dispose();Model.Dispose();uiFont.Dispose();tips.Dispose();if(appIcon!=null)appIcon.Dispose();};Sync();InstallHints(this);
            Shown+=delegate{Rectangle area=Screen.FromControl(this).WorkingArea;if(Height>area.Height-30)Height=Math.Max(MinimumSize.Height,area.Height-30);if(Width>area.Width-30)Width=Math.Max(MinimumSize.Width,area.Width-30);};
        }
        FlowLayoutPanel Page(TabControl tabs,string text){TabPage page=new TabPage(text){BackColor=surface,Padding=new Padding(0)};FlowLayoutPanel p=Column();p.Dock=DockStyle.Fill;page.Controls.Add(p);tabs.TabPages.Add(page);return p;}
        FlowLayoutPanel Column(){return new FlowLayoutPanel{FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=new Padding(8,4,8,18),BackColor=surface};}
        int FieldWidth(FlowLayoutPanel p){return 300;}
        void Section(FlowLayoutPanel p,string text){p.Controls.Add(new Label{Text=text,Width=FieldWidth(p),Height=37,Padding=new Padding(0,9,0,0),Margin=new Padding(0,9,0,3),ForeColor=Color.FromArgb(45,45,51)});}
        void Note(FlowLayoutPanel p,string text){p.Controls.Add(new Label{Text=text,Width=FieldWidth(p),Height=29,ForeColor=muted,Margin=new Padding(0,5,0,0)});}
        SoftButton Button(string text,FlowLayoutPanel p,bool primary=false){SoftButton b=new SoftButton{Text=text,Primary=primary,Width=FieldWidth(p),Height=41,Margin=new Padding(0,3,0,7)};if(p!=null)p.Controls.Add(b);return b;}
        void Input(FlowLayoutPanel p,Func<string> get,Action<string> set,string group,bool multiline){TextBox t=new TextBox{Width=FieldWidth(p),MaxLength=multiline?240:60,Multiline=multiline,Height=multiline?78:30,BorderStyle=BorderStyle.FixedSingle,Margin=new Padding(0,3,0,6),AcceptsReturn=multiline,ScrollBars=multiline?ScrollBars.Vertical:ScrollBars.None};p.Controls.Add(t);bindings.Add(delegate{string shown=multiline?get().Replace("\r\n","\n").Replace("\n","\r\n"):get();if(t.Text!=shown)t.Text=shown;});t.TextChanged+=delegate{Edit(delegate{set(t.Text);},group+selectedId);};}
        void Number(FlowLayoutPanel p,string label,int min,int max,Func<int> get,Action<int> set,string group){Note(p,label);NumberControl c=new NumberControl(min,max){Width=FieldWidth(p),Margin=new Padding(0,0,0,3),AccessibleName=label};p.Controls.Add(c);bindings.Add(delegate{c.Value=get();});c.Changed+=delegate(int v){Edit(delegate{set(v);},group+selectedId);};}
        void Check(FlowLayoutPanel p,string label,Func<bool> get,Action<bool> set){CheckBox c=new CheckBox{Text=label,AutoSize=true,Margin=new Padding(0,8,0,7)};p.Controls.Add(c);bindings.Add(delegate{c.Checked=get();});c.CheckedChanged+=delegate{Edit(delegate{set(c.Checked);},null);};}
        void Choice(FlowLayoutPanel p,string[] values,Func<int> get,Action<int> set){ComboBox c=new QuietComboBox{Width=FieldWidth(p),DropDownStyle=ComboBoxStyle.DropDownList,FlatStyle=FlatStyle.Flat,Margin=new Padding(0,3,0,7)};c.Items.AddRange(values);p.Controls.Add(c);bindings.Add(delegate{c.SelectedIndex=get();});c.SelectedIndexChanged+=delegate{if(c.SelectedIndex>=0)Edit(delegate{set(c.SelectedIndex);},null);};}
        void FontChoice(FlowLayoutPanel p,Func<string> get,Action<string> set){
            SoftButton button=Button("",p);button.AccessibleName="폰트 미리보기";
            bindings.Add(delegate{button.Text=get()+"  ▾";});
            button.Click+=delegate{
                Card before=Model.Snapshot();bool accepted=false;
                try{using(FontPicker picker=new FontPicker(get(),p==bubbleFields?Selected.Text:Model.Name+"\n카톡테마 공유")){
                    PlacePicker(picker);picker.PreviewChanged+=delegate(string font){set(font);preview.Refresh();};
                    if(picker.ShowDialog(this)==DialogResult.OK){set(picker.Result);accepted=true;}
                }}finally{if(!accepted)Model.Restore(before);}
                if(accepted)History.Record(before,null);Sync();
            };
        }
        void PaintButton(FlowLayoutPanel p,string title,Func<PaintSpec> get,Action<PaintSpec> set){ColorRow(p,title,get,set,true);}
        void SolidButton(FlowLayoutPanel p,string title,Func<Color> get,Action<Color> set){ColorRow(p,title,delegate{return new PaintSpec(get(),get());},delegate(PaintSpec paint){set(paint.First);},false);}
        void ColorRow(FlowLayoutPanel p,string title,Func<PaintSpec> get,Action<PaintSpec> set,bool gradient){
            Panel row=new Panel{Width=FieldWidth(p),Height=43,Margin=new Padding(0,3,0,5)};
            Label label=new Label{Text=title,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft};
            ColorChip chip=new ColorChip{Dock=DockStyle.Right,Width=46,AccessibleName=title+" 선택"};row.Controls.Add(label);row.Controls.Add(chip);p.Controls.Add(row);
            bindings.Add(delegate{chip.FillSpec=get().Copy();chip.Invalidate();});
            tips.SetToolTip(chip,title+" · 클릭하면 실시간 색상 미리보기");
            chip.Click+=delegate{
                if(activeColor!=null){activeColor.Activate();return;}
                Card before=Model.Snapshot();ColorPicker picker=new ColorPicker(title,get(),gradient,Model);activeColor=picker;
                PlacePicker(picker);inspectorHost.Enabled=false;headerHost.Enabled=false;
                picker.PreviewChanged+=delegate(PaintSpec paint){set(paint.Copy());chip.FillSpec=paint.Copy();chip.Invalidate();preview.Refresh();};
                picker.SampleRequested+=delegate(Action<Color> accept){
                    picker.Hide();preview.BeginPick(delegate(Color color){accept(color);picker.Show(this);picker.Activate();},delegate{picker.Show(this);picker.Activate();});
                };
                picker.FormClosed+=delegate{
                    preview.StopPick();bool accepted=picker.DialogResult==DialogResult.OK;
                    if(accepted)set(picker.Result.Copy());else Model.Restore(before);
                    activeColor=null;inspectorHost.Enabled=true;headerHost.Enabled=true;
                    if(accepted)History.Record(before,null);Sync();
                };
                picker.Show(this);
            };
        }
        void TextStyle(FlowLayoutPanel p,Func<TextLook> get){
            Check(p,"볼드체",delegate{return get().Bold;},delegate(bool v){get().Bold=v;});
            Check(p,"기울기체",delegate{return get().Italic;},delegate(bool v){get().Italic=v;});
            Check(p,"글자 테두리",delegate{return get().Outline;},delegate(bool v){get().Outline=v;});
            SolidButton(p,"테두리 색",delegate{return get().OutlineColor;},delegate(Color c){get().OutlineColor=c;});
            Number(p,"테두리 두께",0,48,delegate{return get().Stroke;},delegate(int v){get().Stroke=v;},"letter-stroke");
            Check(p,"글자 그림자",delegate{return get().Shadow;},delegate(bool v){get().Shadow=v;});
            Number(p,"그림자 거리",0,35,delegate{return get().ShadowDistance;},delegate(int v){get().ShadowDistance=v;},"letter-shadow-distance");
            Number(p,"그림자 진하기",0,220,delegate{return get().ShadowOpacity;},delegate(int v){get().ShadowOpacity=v;},"letter-shadow-opacity");
            Check(p,"네온처럼 퍼지는 빛",delegate{return get().Glow;},delegate(bool v){get().Glow=v;});
            SolidButton(p,"네온 색",delegate{return get().GlowColor;},delegate(Color c){get().GlowColor=c;});
            Number(p,"빛이 퍼지는 크기",2,60,delegate{return get().GlowRadius;},delegate(int v){get().GlowRadius=v;},"letter-glow-radius");
            Number(p,"네온 진하기",0,220,delegate{return get().GlowOpacity;},delegate(int v){get().GlowOpacity=v;},"letter-glow-opacity");
        }
        void PlacePicker(Form picker){
            picker.StartPosition=FormStartPosition.Manual;Rectangle area=Screen.FromControl(this).WorkingArea;
            picker.Location=new Point(Math.Max(area.Left,Math.Min(Left+18,area.Right-picker.Width)),Math.Max(area.Top,Math.Min(Top+85,area.Bottom-picker.Height)));
        }
        void InstallHints(Control root){
            foreach(Control c in root.Controls){
                if(c is NumberControl){foreach(Control part in c.Controls)tips.SetToolTip(part,c.AccessibleName+" · 클릭/드래그, 화살표 또는 숫자 입력으로 조절");}
                else if(c is SoftButton||c is ComboBox||c is TextBox){if(String.IsNullOrEmpty(tips.GetToolTip(c)))tips.SetToolTip(c,String.IsNullOrEmpty(c.AccessibleName)?c.Text:c.AccessibleName);}
                InstallHints(c);
            }
        }
        void Edit(Action action,string group){if(!syncing&&activeColor==null)History.Change(action,group);}
        void Sync(){syncing=true;try{if(Model.Find(selectedId)==null)selectedId=null;preview.SelectedId=selectedId;foreach(Action b in bindings)b();undo.Enabled=History.CanUndo;redo.Enabled=History.CanRedo;bubbleFields.Enabled=selectedId!=null;SyncBubbleList();preview.Invalidate();}finally{syncing=false;}}
        void SyncBubbleList(){bool same=bubbleList.Items.Count==Model.Bubbles.Count;if(same)for(int i=0;i<Model.Bubbles.Count;i++){BubbleItem item=(BubbleItem)bubbleList.Items[i];if(item.Id!=Model.Bubbles[i].Id||item.Text!=Model.Bubbles[i].Text){same=false;break;}}if(!same){bubbleList.Items.Clear();for(int i=0;i<Model.Bubbles.Count;i++)bubbleList.Items.Add(new BubbleItem{Id=Model.Bubbles[i].Id,Text=Model.Bubbles[i].Text,Index=i+1});}bubbleList.SelectedIndex=Model.Bubbles.FindIndex(delegate(Bubble b){return b.Id==selectedId;});}
        void Select(string id){selectedId=id;if(id!=null)inspectorTabs.SelectedIndex=4;Sync();}
        public void AddBubble(){if(Model.Bubbles.Count>=30){status.Text="말풍선은 최대 30개까지 추가할 수 있어요.";return;}Bubble b=new Bubble{X=540,Y=Math.Min(Model.Height-130,720+Model.Bubbles.Count*28)};selectedId=b.Id;Edit(delegate{Model.Bubbles.Add(b);},null);status.Text="말풍선을 드래그해 배치하세요. 오른쪽에서 문구와 폰트를 바꿀 수 있어요.";}
        public void DeleteBubble(){Bubble b=Model.Find(selectedId);if(b==null)return;selectedId=null;Edit(delegate{Model.Bubbles.Remove(b);},null);}
        void DuplicateBubble(){Bubble old=Model.Find(selectedId);if(old==null||Model.Bubbles.Count>=30)return;Bubble b=old.Copy();b.Id=Guid.NewGuid().ToString("N");b.X=Math.Min(1000,b.X+30);b.Y=Math.Min(Model.Height-50,b.Y+30);selectedId=b.Id;Edit(delegate{Model.Bubbles.Add(b);},null);}
        void Reorder(bool front){Bubble b=Model.Find(selectedId);if(b==null)return;Edit(delegate{Model.Bubbles.Remove(b);if(front)Model.Bubbles.Add(b);else Model.Bubbles.Insert(0,b);},null);}
        protected override bool ProcessCmdKey(ref Message msg,Keys keys){
            if(activeColor!=null){if(keys==Keys.Escape&&preview.IsSampling){preview.CancelPick();return true;}if((keys&Keys.Control)==Keys.Control)return true;return base.ProcessCmdKey(ref msg,keys);}
            if(keys==(Keys.Control|Keys.Z)){History.Undo();return true;}if(keys==(Keys.Control|Keys.Y)||keys==(Keys.Control|Keys.Shift|Keys.Z)){History.Redo();return true;}
            if(keys==(Keys.Control|Keys.O)){OpenPhoto();return true;}if(keys==(Keys.Control|Keys.S)){Export();return true;}
            if(keys==(Keys.Control|Keys.V)&&!(ActiveControl is TextBox)){Paste();return true;}
            if(preview.Focused){if(keys==Keys.Delete){DeleteBubble();return true;}int step=(keys&Keys.Shift)==Keys.Shift?10:1;Keys k=keys&Keys.KeyCode;if(k==Keys.Left||k==Keys.Right||k==Keys.Up||k==Keys.Down){Bubble b=Model.Find(selectedId);if(b!=null)Edit(delegate{if(k==Keys.Left)b.X=Math.Max(0,b.X-step);if(k==Keys.Right)b.X=Math.Min(1080,b.X+step);if(k==Keys.Up)b.Y=Math.Max(0,b.Y-step);if(k==Keys.Down)b.Y=Math.Min(Model.Height,b.Y+step);},"nudge");return true;}}
            return base.ProcessCmdKey(ref msg,keys);
        }
        void OpenPhoto(){using(OpenFileDialog d=new OpenFileDialog{Title="카톡테마 캡처",Filter="이미지|*.png;*.jpg;*.jpeg;*.bmp;*.gif"})if(d.ShowDialog(this)==DialogResult.OK)LoadPhoto(d.FileName);}
        public bool LoadPhoto(string path){try{Edit(delegate{Model.Load(path);},null);status.Text="캡처를 불러왔어요. 사진 교체도 Ctrl+Z로 취소할 수 있어요.";return true;}catch(Exception e){MessageBox.Show(this,"이미지를 열 수 없어요. PNG/JPG 파일을 확인해 주세요.\n"+e.Message);return false;}}
        void Paste(){try{if(!Clipboard.ContainsImage()){status.Text="캡처를 복사한 뒤 다시 눌러주세요.";return;}using(Image image=Clipboard.GetImage()){if(image==null)return;string path=Path.Combine(Path.GetTempPath(),"kongeuni-"+Guid.NewGuid().ToString("N")+".png");try{image.Save(path,ImageFormat.Png);LoadPhoto(path);}finally{if(File.Exists(path))File.Delete(path);}}}catch(Exception e){MessageBox.Show(this,e.Message,"붙여넣기");}}
        void Export(){if(Model.IsDemo&&MessageBox.Show(this,"회색 자리표시자 상태로 저장할까요?","예시 이미지",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;string file=(String.IsNullOrWhiteSpace(Model.Name)?"나의 테마":Model.Name)+" 카톡테마 공유";foreach(char c in Path.GetInvalidFileNameChars())file=file.Replace(c,'_');using(SaveFileDialog d=new SaveFileDialog{Filter="PNG 이미지|*.png",FileName=file+".png",DefaultExt="png",AddExtension=true,OverwritePrompt=true})if(d.ShowDialog(this)==DialogResult.OK)try{using(Bitmap b=Model.Render())b.Save(d.FileName,ImageFormat.Png);status.Text="저장 완료 · "+d.FileName;}catch(Exception e){MessageBox.Show(this,e.Message,"저장");}}
        sealed class BubbleItem{public string Id,Text;public int Index;public override string ToString(){string text=Text.Replace("\r","").Replace("\n"," ");return Index+" · "+(text.Length>15?text.Substring(0,15)+"…":text);}}
    }
}
