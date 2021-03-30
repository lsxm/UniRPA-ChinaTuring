using System;
using System.Activities;
using System.Activities.Presentation.Metadata;
using System.Collections.Generic;
using System.ComponentModel;
using System.Activities.Presentation.PropertyEditing;
using System.Security;
using System.Windows;
using MouseActivity;
using System.Runtime.InteropServices;
using Plugins.Shared.Library.UiAutomation;
using Plugins.Shared.Library;
using System.Threading;
using System.Windows.Forms;
using Plugins.Shared.Library.Exceptions;
using Plugins.Shared.Library.WindowsAPI;

namespace KeyboardActivity
{
    [Designer(typeof(SecureTextDesigner))]
    public sealed class SecureTextActivity : CodeActivity
    {
        static SecureTextActivity()
        {
            AttributeTableBuilder builder = new AttributeTableBuilder();
            builder.AddCustomAttributes(typeof(SecureTextActivity), "ClickType", new EditorAttribute(typeof(MouseClickTypeEditor), typeof(PropertyValueEditor)));
            builder.AddCustomAttributes(typeof(SecureTextActivity), "MouseButton", new EditorAttribute(typeof(MouseButtonTypeEditor), typeof(PropertyValueEditor)));
            //builder.AddCustomAttributes(typeof(SecureTextActivity), "KeyModifiers", new EditorAttribute(typeof(KeyModifiersEditor), typeof(PropertyValueEditor)));
            MetadataStore.AddAttributeTable(builder.CreateTable());
        }


        #region ���Է��ࣺ����

        public string _displayName;
        [Category("����")]
        [DisplayName("��ʾ����")]
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

        [Category("����")]
        [DisplayName("����ʱ����")]
        [Description("ָ����ʹ�ڵ�ǰ�ʧ�ܵ�����£��Լ���ִ��ʣ��Ļ����֧�ֲ���ֵ��True,False����")]
        public bool ContinueOnError { get; set; }

        [Category("����")]
        [DisplayName("�ڴ�֮ǰ�ӳ�")]
        [Description("���ʼִ���κβ���֮ǰ���ӳ�ʱ�䣨�Ժ���Ϊ��λ����Ĭ��ʱ����Ϊ200���롣")]
        public InArgument<int> DelayBefore { get; set; }

        [Category("����")]
        [DisplayName("�ڴ�֮���ӳ�")]
        [Description("ִ�л֮����ӳ�ʱ�䣨�Ժ���Ϊ��λ����Ĭ��ʱ����Ϊ300���롣")]
        public InArgument<int> DelayAfter { get; set; }

        #endregion


        #region ���Է��ࣺĿ��

        [Category("Ŀ��")]
        [OverloadGroup("G1")]
        [DisplayName("Ԫ��")]
        [Description("ʹ����һ������ص��û�����Ԫ�ر����� �����Բ����롰ѡȡ��������һ��ʹ�á����ֶν�֧���û�����Ԫ�ر�����")]
        public InArgument<UiElement> Element { get; set; }

        [Category("Ŀ��")]
        [OverloadGroup("G1")]
        [DisplayName("��ʱ�����룩")]
        [Description("ָ����ȴ�ʱ�䣨�Ժ���Ϊ��λ�������������ʱ��δ���У�ϵͳ�ͻᱨ��Ĭ��ֵΪ30000���루30�룩��")]
        public InArgument<int> Timeout { get; set; }

        [Category("Ŀ��")]
        [OverloadGroup("G1")]
        [DisplayName("ѡȡ��")]
        [Description("������ִ�лʱ�����ض��û�����Ԫ�صġ��ı������ԡ���ʵ������XMLƬ�Σ�����ָ����Ҫ���ҵ�ͼ���û�����Ԫ�ؼ���һЩ��Ԫ�ص����ԡ����뽫�ı����������С�")]
        public InArgument<string> Selector { get; set; }
        [Browsable(false)]
        public string SelectorOrigin { get; set; }

        #endregion


        #region ���Է��ࣺ���λ��

        [Category("���λ��")]
        [DisplayName("ƫ�� X")]
        [Description("���λ�ô�Ԫ�����ĵ���е�ˮƽλ�ơ�")]
        public InArgument<int> offsetX { get; set; }

        [Category("���λ��")]
        [DisplayName("ƫ�� Y")]
        [Description("���λ�ô�Ԫ�����ĵ���еĴ�ֱλ�ơ�")]
        public InArgument<int> offsetY { get; set; }

        #endregion


        #region ���Է��ࣺ����

        [Category("����")]
        [RequiredArgument]
        [DisplayName("��ȫ�ı�")]
        [Description("��д��ָ���û�����Ԫ�صİ�ȫ�ı������뽫�ı����������С�")]
        public InArgument<SecureString> SecureText { get; set; }

        #endregion


        #region ���Է��ࣺѡ��
        [Category("ѡ��")]
        [DisplayName("ģ�����")]
        [Description("���ѡ�У���ͨ��ʹ��Ŀ��Ӧ�ó���ļ���ģ�����͡��������뷽�������ַ��������ģ��ҿ��ں�̨������Ĭ������£��ø�ѡ����δѡ��״̬�������δѡ�иø�ѡ��Ҳδѡ�С����ʹ�����Ϣ����ѡ����Ĭ�Ϸ���ͨ��ʹ��Ӳ����������ִ�е����Ĭ�Ϸ����ٶ��������Ҳ����ں�̨���������ɼ�����������Ӧ�ó���")]
        public bool SimulateInput { get; set; }

        [Category("ѡ��")]
        [DisplayName("���ֶ�")]
        [Description("ѡ�иø�ѡ��ʱ��ϵͳ����д���ı�ǰ����û�����Ԫ��������֮ǰ���ڵ����ݡ�")]
        public bool EmptyText { get; set; }

        [Category("ѡ��")]
        [DisplayName("��֮���ӳ�")]
        [Description("���λ���֮����ӳ�ʱ�䣨�Ժ���Ϊ��λ����Ĭ��ʱ����Ϊ 10 ���롣")]
        public InArgument<int> DelayBetweenInputs { get; set; }

        [Category("ѡ��")]
        [DisplayName("����ǰ����")]
        [Description("ѡ�иø�ѡ��ʱ����д���ı�֮ǰ����ָ���û�����Ԫ�ء�")]
        public bool IsRunClick { get; set; }

        //[Category("ѡ��")]
        //[DisplayName("���μ�")]
        //[Description("ʹ���ܹ�������μ������õ�ѡ�����£�Alt��Ctrl��Shift��Win��")]
        //public string KeyModifiers { get; set; }

        #endregion


        #region ���Է��ࣺ����

        [Browsable(false)]
        public string icoPath { get { return "pack://application:,,,/Plugins.Shared.Library;Component/Resource/Image/Activities/hotkey/text.png"; } }

        [Browsable(false)]
        public List<string> KeyTypes
        {
            get
            {
                KeyboardTypes key = new KeyboardTypes();
                return key.getKeyTypes;
            }
        }

        [Browsable(false)]
        public string SourceImgPath { get; set; }

        private System.Windows.Visibility visi = System.Windows.Visibility.Hidden;
        [Browsable(false)]
        public System.Windows.Visibility visibility
        {
            get
            {
                return visi;
            }
            set
            {
                visi = value;
            }
        }

        [Browsable(false)]
        public string DefaultName { get { return "���밲ȫ�ı�"; } }

        #endregion

        void ParseStringToList(string inText, List<string> strList)
        {
            string strBuff = "";
            string keyBuff = "";
            bool isKeyFlag = false;
            for (int counter = 0; counter < inText.Length; counter++)
            {
                if (counter < inText.Length - 1)
                {
                    if (inText[counter] == '[' && inText[counter + 1] == 'k')
                    {
                        isKeyFlag = true;
                    }
                    if (inText[counter] == ')' && inText[counter + 1] == ']')
                    {
                        isKeyFlag = false;
                    }
                }
                if (isKeyFlag)
                {
                    keyBuff += inText[counter].ToString();
                    if (strBuff != "")
                    {
                        strBuff = strBuff.Replace("[k(", "");
                        strBuff = strBuff.Replace(")]", "");
                        strList.Add(strBuff);
                        strBuff = "";
                    }
                }
                else
                {
                    strBuff += inText[counter].ToString();

                    if (keyBuff != "")
                    {
                        keyBuff = keyBuff.Replace("[k(", "");
                        keyBuff = keyBuff.Replace("[k(", "");
                        keyBuff = "[(" + keyBuff + ")]";
                        strList.Add(keyBuff);
                        keyBuff = "";
                    }
                }

                if (counter == inText.Length - 1 && inText[counter] != ']')
                {
                    strBuff = strBuff.Replace("[k(", "");
                    strBuff = strBuff.Replace(")]", "");
                    strList.Add(strBuff);
                }
            }
        }

        protected override void Execute(CodeActivityContext context)
        {
            int delayAfter = Common.GetValueOrDefault(context, this.DelayAfter, 300);
            int delayBefore = Common.GetValueOrDefault(context, this.DelayBefore, 200);
            Thread.Sleep(delayBefore);
            var delayBetweenInputs = Common.GetValueOrDefault(context, DelayBetweenInputs, 10);

            try
            {
                SecureString secureText = SecureText.Get(context);
                IntPtr inP = Marshal.SecureStringToBSTR(secureText);//inPΪsecureStr�ľ��
                string text = Marshal.PtrToStringBSTR(inP);

                List<string> strList = new List<string>();
                ParseStringToList(text, strList);



                int timeout = Common.GetValueOrDefault(context, this.Timeout, 30000);
                var selStr = Selector.Get(context);
                UiElement element = Common.GetValueOrDefault(context, this.Element, null);
                if (element == null && selStr != null)
                {
                    element = UiElement.FromSelector(selStr, timeout);
                }

                int pointX = offsetX.Get(context);
                int pointY = offsetY.Get(context);
                if (element != null)
                {
                    element.SetForeground();
                }
                else
                {
                    SharedObject.Instance.Output(SharedObject.OutputType.Error, "��һ���������", "���Ҳ���Ԫ��");
                    if (ContinueOnError)
                    {
                        return;
                    }
                    else
                    {
                        throw new NotImplementedException("���Ҳ���Ԫ��");
                    }
                }
                /*ִ��������¼�*/
                if (IsRunClick)
                {
                    UiElementClickParams uiElementClickParams = new UiElementClickParams();
                    Offset offset = new Offset(pointX, pointY);
                    uiElementClickParams.offset = offset;
                    element.MouseClick(uiElementClickParams);
                }
                IntPtr windowHandle = IntPtr.Zero;
                windowHandle = element.MainWindowHandle;

                using (var imeScope = IMEScope.BeginEdit(windowHandle, IMEHelper.HKL_ENGLISH_US))
                {
                    element.Focus();
                    //�����������
                    if (EmptyText)
                    {
                        UiKeyboard.New().Press(VirtualKey.CONTROL).With(VirtualKey.KEY_A).Continue(VirtualKey.DELETE).End();
                    }

                    foreach (string str in strList)
                    {
                        string strValue = str;
                        if (strValue.Contains("[(") && strValue.Contains(")]"))
                        {
                            strValue = strValue.Replace("[(", "");
                            strValue = strValue.Replace(")]", "");
                            if (SimulateInput)
                            {
                                Thread.Sleep(delayBetweenInputs);
                            }

                            if (Common.DealVirtualKeyPress(strValue.ToUpper()))
                            {
                                Common.DealVirtualKeyRelease(strValue.ToUpper());
                            }
                            else
                            {
                                SharedObject.Instance.Output(SharedObject.OutputType.Error, "��һ���������", "�Ҳ�����ֵ");
                                if (ContinueOnError)
                                {
                                    return;
                                }
                                else
                                {
                                    throw new NotImplementedException("�Ҳ�����ֵ");
                                }
                            }
                        }
                        else if (!string.IsNullOrWhiteSpace(strValue))
                        {

                            if (!SimulateInput)
                            {
                                foreach (var item in strValue)
                                {
                                    Thread.Sleep(delayBetweenInputs);
                                    SendKeys.SendWait(item.ToString());
                                }
                            }
                            else
                            {
                                element.SimulateTypeText(strValue);
                            }
                        }
                    }

                }

                Thread.Sleep(delayAfter);
            }
            catch (Exception e)
            {
                SharedObject.Instance.Output(SharedObject.OutputType.Error, DisplayName + "ʧ��", e.Message);
                if (!ContinueOnError)
                {
                    throw new ActivityRuntimeException(this.DisplayName, e);
                }
            }
        }
    }
}
