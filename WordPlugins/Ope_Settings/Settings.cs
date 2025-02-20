﻿using System.Activities;
using System.ComponentModel;
using System;
using Word = Microsoft.Office.Interop.Word;
using Plugins.Shared.Library;
using ExcelPlugins;
using System.Threading;
using MouseActivity;
using Plugins.Shared.Library.Exceptions;

namespace WordPlugins
{
    [Designer(typeof(SettingsDesigner))]
    public sealed class Settings : CodeActivity
    {
        #region 属性分类：常见

        public string _displayName;
        [Category("常见")]
        [DisplayName("显示名称")]
        public new string DisplayName
        {
            get
            {
                if (_displayName == null)
                {
                    _displayName = base.DisplayName;
                }
                else
                {
                    base.DisplayName = _displayName;
                }

                return _displayName;
            }
            set
            {
                _displayName = value;
            }
        }
        [Browsable(false)]
        public string _DisplayName
        {
            get
            {
                return this.DisplayName;
            }
        }

        [Category("常见")]
        [DisplayName("出错时继续")]
        [Description("指定即使在当前活动失败的情况下，仍继续执行剩余的活动。仅支持布尔值（True,False）。")]
        public bool ContinueOnError { get; set; }

        [Category("常见")]
        [DisplayName("在此之前延迟")]
        [Description("活动开始执行任何操作之前的延迟时间（以毫秒为单位）。默认时间量为200毫秒。")]
        public InArgument<int> DelayBefore { get; set; }

        [Category("常见")]
        [DisplayName("在此之后延迟")]
        [Description("执行活动之后的延迟时间（以毫秒为单位）。默认时间量为300毫秒。")]
        public InArgument<int> DelayAfter { get; set; }

        #endregion


        #region 属性分类：字体设置

        ExcelFontEnum _Font;
        [Category("字体设置")]
        [DisplayName("字体")]
        public ExcelFontEnum Font
        {
            get { return _Font; }
            set { _Font = value; }
        }

        InArgument<float> _FontSize = 10.5F;
        [Category("字体设置")]
        [DisplayName("字体大小")]
        public InArgument<float> FontSize
        {
            get { return _FontSize; }
            set { _FontSize = value; }
        }

        WdColorIndexEnum _FontColor;
        [Category("字体设置")]
        [DisplayName("字体颜色")]
        public WdColorIndexEnum FontColor
        {
            get { return _FontColor; }
            set { _FontColor = value; }
        }

        bool _FontBold = false;
        [Category("字体设置")]
        [DisplayName("粗体")]
        public bool FontBold
        {
            get { return _FontBold; }
            set { _FontBold = value; }
        }


        bool _FontItalic = false;
        [Category("字体设置")]
        [DisplayName("斜体")]
        public bool FontItalic
        {
            get { return _FontItalic; }
            set { _FontItalic = value; }
        }

        bool _FontUnderLine = false;
        [Category("字体设置")]
        [DisplayName("下划线")]
        public bool FontUnderLine
        {
            get { return _FontUnderLine; }
            set { _FontUnderLine = value; }
        }

        bool _Shadow = false;
        [Category("字体设置")]
        [DisplayName("字体阴影")]
        public bool Shadow
        {
            get { return _Shadow; }
            set { _Shadow = value; }
        }

        #endregion


        #region 属性分类：页面属性

        public enum alignStyle
        {
            左对齐 = 0,
            居中 = 1,
            右对齐 = 2
        }
        alignStyle _Align = alignStyle.左对齐;
        [Category("页面属性")]
        [DisplayName("对齐方式")]
        [Browsable(true)]
        public alignStyle Align
        {
            get { return _Align; }
            set { _Align = value; }
        }

        #endregion


        #region 属性分类：页面边距

        InArgument<Int32> _LeftMargin = 80;
        [Category("页面边距")]
        [DisplayName("左边距")]
        [Browsable(true)]
        public InArgument<Int32> LeftMargin
        {
            get { return _LeftMargin; }
            set { _LeftMargin = value; }
        }

        InArgument<Int32> _RightMargin = 80;
        [Category("页面边距")]
        [DisplayName("右边距")]
        [Browsable(true)]
        public InArgument<Int32> RightMargin
        {
            get { return _RightMargin; }
            set { _RightMargin = value; }
        }

        InArgument<Int32> _TopMargin = 64;
        [Category("页面边距")]
        [DisplayName("上边距")]
        [Browsable(true)]
        public InArgument<Int32> TopMargin
        {
            get { return _TopMargin; }
            set { _TopMargin = value; }
        }

        InArgument<Int32> _BottomMargin = 64;
        [Category("页面边距")]
        [DisplayName("下边距")]
        [Browsable(true)]
        public InArgument<Int32> BottomMargin
        {
            get { return _BottomMargin; }
            set { _BottomMargin = value; }
        }

        #endregion


        #region 属性分类：杂项

        [Browsable(false)]
        public string icoPath { get { return "pack://application:,,,/Plugins.Shared.Library;Component/Resource/Image/Activities/word/property.png"; } }

        #endregion


        public Settings()
        {
        }

        public string ConvertFont(string fontName)
        {
            if (fontName == "等线Light")
                return "等线 Light";
            else if (fontName == "ArialBlack")
                return "Arial Black";
            else if (fontName == "ArialNarrow")
                return "Arial Narrow";
            else if (fontName == "ArialRoundedMTBold")
                return "Arial Rounded MT Bold";
            else if (fontName == "ArialUnicodeMS")
                return "Arial Unicode MS";
            else if (fontName == "CalibriLight")
                return "Calibri Light";
            else if (fontName == "MicrosoftYaHeiUI")
                return "Microsoft YaHei UI";
            else if (fontName == "MicrosoftYaHeiUILight")
                return "Microsoft YaHei UI Light";
            else if (fontName == "MicrosoftJhengHei")
                return "Microsoft JhengHei";
            else if (fontName == "MicrosoftJhengHeiLight")
                return "Microsoft JhengHei Light";
            else if (fontName == "MicrosoftJhengHeiUI")
                return "Microsoft JhengHei UI";
            else if (fontName == "MicrosoftJhengHeiUILight")
                return "Microsoft JhengHei UI Light";
            else if (fontName == "MicrosoftMHei")
                return "Microsoft MHei";
            else if (fontName == "MicrosoftNeoGothic")
                return "Microsoft NeoGothic";
            else if (fontName == "MalgunGothic")
                return "Malgun Gothic";
            else if (fontName == "AgencyFB")
                return "Agency FB";
            else if (fontName == "Bauhaus93")
                return "Bauhaus 93";
            else if (fontName == "BellMT")
                return "Bell MT";
            else if (fontName == "BerlinSansFB")
                return "Berlin Sans FB";
            else if (fontName == "BerlinSansFBDemi")
                return "Berlin Sans FB Demi";
            else if (fontName == "BernardMTCondensed")
                return "Bernard MT Condensed";
            else if (fontName == "BlackadderITC")
                return "Blackadder ITC";
            else if (fontName == "BodoniMT")
                return "Bodoni MT";
            else if (fontName == "BodoniMTBlack")
                return "Bodoni MT Black";
            else if (fontName == "YuGothic")
                return "Yu Gothic";
            else
                return fontName;
        }

        protected override void Execute(CodeActivityContext context)
        {
            int delayAfter = Common.GetValueOrDefault(context, this.DelayAfter, 300);
            int delayBefore = Common.GetValueOrDefault(context, this.DelayBefore, 200);
            Thread.Sleep(delayBefore);

            try
            {
                float fontSize = FontSize.Get(context);
                Int32 leftMargin = LeftMargin.Get(context);
                Int32 rightMargin = RightMargin.Get(context);
                Int32 topMargin = TopMargin.Get(context);
                Int32 bottomMargin = BottomMargin.Get(context);
                CommonVariable.sel = CommonVariable.app.Selection;
                CommonVariable.font = CommonVariable.sel.Font;

                /*字体设置*/
                CommonVariable.font.Size = fontSize;
                if (Font != 0)
                    CommonVariable.font.Name = ConvertFont(Font.ToString());
                CommonVariable.font.ColorIndex = (Word.WdColorIndex)_FontColor;
                CommonVariable.font.Shadow = Convert.ToInt32(_Shadow);
                CommonVariable.font.Bold = Convert.ToInt32(_FontBold);
                CommonVariable.font.Italic = Convert.ToInt32(_FontItalic);
                if (_FontUnderLine)
                    CommonVariable.font.Underline = Word::WdUnderline.wdUnderlineSingle;

                /*段落对齐设置*/
                Word::ParagraphFormat paraFmt;
                paraFmt = CommonVariable.sel.ParagraphFormat;
                paraFmt.Alignment = (Word::WdParagraphAlignment)_Align;
                CommonVariable.sel.ParagraphFormat = paraFmt;

                /*页面设置*/
                CommonVariable.doc = CommonVariable.app.ActiveDocument;
                Word::PageSetup pgSet = CommonVariable.doc.PageSetup;
                pgSet.LeftMargin = leftMargin;
                pgSet.RightMargin = rightMargin;
                pgSet.TopMargin = topMargin;
                pgSet.BottomMargin = bottomMargin;
            }
            catch (Exception e)
            {
                SharedObject.Instance.Output(SharedObject.OutputType.Error, DisplayName + "失败", e.Message);
                CommonVariable.realaseProcessExit();
                if (!ContinueOnError)
                {
                    throw new ActivityRuntimeException(this.DisplayName, e);
                }
            }

            Thread.Sleep(delayAfter);
        }
    }
}
