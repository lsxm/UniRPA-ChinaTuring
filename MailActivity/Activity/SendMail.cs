﻿using System;
using System.Activities;
using System.ComponentModel;
using Plugins.Shared.Library;
using System.Diagnostics;
using MailKit.Security;
using MailKit.Net.Smtp;
using MimeKit;
using System.Collections;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.IO;
using MimeKit.Text;
using System.Net.Mail;
using System.Threading;
using MouseActivity;
using Plugins.Shared.Library.Exceptions;

namespace MailActivity
{
    [Designer(typeof(SendMailDesigner))]
    public sealed class SendMail : CodeActivity
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


        #region 属性分类：登录

        [Category("登录")]
        [RequiredArgument]
        [DisplayName("邮件账户")]
        [Description("用于发送邮件的电子邮件帐户。必须将文本放入引号中。")]
        public InArgument<string> Email { get; set; }

        [Category("登录")]
        [RequiredArgument]
        [DisplayName("密码")]
        [Description("用于发送邮件的电子邮件帐户的密码。必须将文本放入引号中。")]
        public InArgument<string> Password { get; set; }

        #endregion


        #region 属性分类：主机

        [Category("主机")]
        [RequiredArgument]
        [DisplayName("服务器")]
        [Description("使用的电子邮件服务器主机。必须将文本放入引号中。")]
        public InArgument<string> Server { get; set; }

        [Category("主机")]
        [DisplayName("端口")]
        [Description("电子邮件将通过的端口。")]
        public InArgument<Int32> Port { get; set; }

        #endregion


        #region 属性分类：寄件人

        [Category("寄件人")]
        [DisplayName("名称")]
        [Description("发件人的显示名称。必须将文本放入引号中。")]
        public InArgument<string> Name { get; set; }

        [Category("寄件人")]
        [DisplayName("发件人")]
        [Description("发件人的电子邮件地址。必须将文本放入引号中。")]
        public InArgument<string> From { get; set; }

        #endregion


        #region 属性分类：收件人

        [Category("收件人")]
        [RequiredArgument]
        [DisplayName("收件人")]
        [Description("电子邮件的主要收件人。")]
        public InArgument<string[]> Receivers_To { get; set; }

        [Category("收件人")]
        [DisplayName("抄送")]
        [Description("电子邮件的次要收件人。")]
        public InArgument<string[]> Receivers_Cc { get; set; }

        [Category("收件人")]
        [DisplayName("密件抄送")]
        [Description("电子邮件的隐藏收件人。")]
        public InArgument<string[]> Receivers_Bcc { get; set; }

        #endregion


        #region 属性分类：电子邮件

        [Category("电子邮件")]
        [DisplayName("主题")]
        [Description("电子邮件的主题。必须将文本放入引号中。")]
        public InArgument<string> MailTopic { get; set; }

        [Category("电子邮件")]
        [DisplayName("正文")]
        [Description("电子邮件的正文。必须将文本放入引号中。")]
        public InArgument<string> MailBody { get; set; }

        #endregion


        #region 属性分类：选项

        [Category("选项")]
        [DisplayName("安全连接")]
        [Description("指定用于连接的 SSL 和/或 TLS 加密。")]
        public SecureSocketOptions SecureConnection { get; set; }

        [Category("选项")]
        [DisplayName("优先级")]
        [Description("邮件的优先级标志。")]
        public MessagePriority msgProperty { get; set; }

        public bool _IsBodyHtml = true;
        [Category("选项")]
        [DisplayName("HTML格式")]
        [Description("指定邮件正文是否以 HTML 格式编写。")]
        public bool IsBodyHtml
        {
            get
            {
                return _IsBodyHtml;
            }
            set
            {
                _IsBodyHtml = value;
            }
        }

        #endregion


        #region 属性分类：附件

        [Category("附件")]
        [DisplayName("文件")]
        [Description("要添加到电子邮件中的附件。")]
        public InArgument<string[]> AttachFiles { get; set; }

        #endregion


        #region 属性分类：转发

        [Category("转发")]
        [DisplayName("消息体")]
        [Description("要转发的消息，该字段仅支持 MailMessage 对象。")]
        public InArgument<MimeMessage> TransMailMessage { get; set; }

        #endregion


        #region 属性分类：杂项

        [Browsable(false)]
        public string icoPath
        {
            get
            {
                return @"pack://application:,,,/Plugins.Shared.Library;Component/Resource/Image/Activities/Mail/SendMail.png";
            }
        }

        #endregion


        protected override void Execute(CodeActivityContext context)
        {
            int delayAfter = Common.GetValueOrDefault(context, this.DelayAfter, 300);
            int delayBefore = Common.GetValueOrDefault(context, this.DelayBefore, 200);
            Thread.Sleep(delayBefore);

            string username = Email.Get(context);               //发送端账号   
            string password = Password.Get(context);            //发送端密码(这个客户端重置后的密码)
            string server = Server.Get(context);                //邮件服务器
            Int32 port = Port.Get(context);                     //端口号
            string from = From.Get(context);                    //发送者地址（一般与发送账号相同）
            string name = Name.Get(context);                    //发送者名称
            string[] receivers_To = Receivers_To.Get(context);  //收件人
            string[] receivers_Cc = Receivers_Cc.Get(context);  //抄送
            string[] recervers_Bcc = Receivers_Bcc.Get(context);//密送
            string mailTopic = MailTopic.Get(context);          //邮件的主题       
            string mailBody = MailBody.Get(context);            //发送的邮件正文  
            string[] attachFiles = AttachFiles.Get(context);    //附件列表
            MimeMessage transMsg = TransMailMessage.Get(context);

            try
            {
                MailKit.Net.Smtp.SmtpClient client = new MailKit.Net.Smtp.SmtpClient();
                client.Connect(server, port, SecureConnection);
                client.Authenticate(username, password);

                var multipart = new Multipart("mixed");

                MimeMessage msg = new MimeMessage();
                if (name == null && from == null)
                    msg.From.Add((new MailboxAddress(username)));
                else if (name == null && from != null)
                    msg.From.Add((new MailboxAddress(from)));
                else if (name != null && from == null)
                    msg.From.Add((new MailboxAddress(name, username)));
                else
                    msg.From.Add((new MailboxAddress(name, from)));

                if (receivers_To != null)
                {
                    foreach (string receiver in receivers_To)
                        msg.To.Add((new MailboxAddress(receiver)));
                }
                if (receivers_Cc != null)
                {
                    foreach (string receiver in receivers_Cc)
                        msg.Cc.Add((new MailboxAddress(receiver)));
                }
                if (recervers_Bcc != null)
                {
                    foreach (string receiver in recervers_Bcc)
                        msg.Bcc.Add((new MailboxAddress(receiver)));
                }


                if (mailBody != null)
                {
                    if (!IsBodyHtml)
                    {
                        var plain = new TextPart(TextFormat.Plain)
                        {
                            Text = mailBody
                        };
                        multipart.Add(plain);
                    }
                    else
                    {
                        var Html = new TextPart(TextFormat.Html)
                        {
                            Text = mailBody
                        };
                        multipart.Add(Html);
                    }
                }
                else if (transMsg != null)
                {
                    if (!IsBodyHtml)
                    {
                        var plain = new TextPart(TextFormat.Plain)
                        {
                            Text = transMsg.TextBody
                        };
                        multipart.Add(plain);
                    }
                    else
                    {
                        var Html = new TextPart(TextFormat.Html)
                        {
                            Text = transMsg.HtmlBody
                        };
                        multipart.Add(Html);
                    }
                }
                else
                {
                    var plain = new TextPart(TextFormat.Plain)
                    {
                        Text = ""
                    };
                    multipart.Add(plain);
                }



                if (mailTopic != null)
                    msg.Subject = mailTopic;
                else if (transMsg != null)
                    msg.Subject = transMsg.Subject;

                msg.Priority = msgProperty;

                if (attachFiles != null)
                {
                    foreach (var p in attachFiles)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(p.Trim()))
                            {
                                var attimg = new MimePart()
                                {
                                    Content = new MimeContent(File.OpenRead(p), ContentEncoding.Default),
                                    ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                                    ContentTransferEncoding = ContentEncoding.Base64,
                                    FileName = Path.GetFileName(p)
                                };
                                multipart.Add(attimg);
                            }
                        }
                        catch (FileNotFoundException ex)
                        {
                            SharedObject.Instance.Output(SharedObject.OutputType.Error, "文件未找到，请检查有效路径", ex.Message);
                        }
                    }
                }
                if (transMsg != null)
                {
                    foreach (MimeEntity part in transMsg.Attachments)
                    {
                        multipart.Add(part);
                    }
                }


                msg.Body = multipart;
                try
                {
                    client.Send(msg);
                    Debug.WriteLine("发送成功");
                }
                catch (System.Net.Mail.SmtpException ex)
                {
                    Debug.WriteLine(ex.Message, "发送邮件出错");
                }
            }
            catch (Exception e)
            {
                SharedObject.Instance.Output(SharedObject.OutputType.Error, DisplayName + "失败", e.Message);
                if (!ContinueOnError)
                {
                    throw new ActivityRuntimeException(this.DisplayName, e);
                }
            }

            Thread.Sleep(delayAfter);
        }
        private void onComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
        }
    }
}