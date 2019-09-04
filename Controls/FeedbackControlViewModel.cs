using System;
using System.IO;
using System.Net.Mail;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using Reactive.Bindings;

namespace uic_addin.Controls {
    internal class FeedbackControlViewModel : CustomControl {
        public FeedbackControlViewModel() {
            HasContent = Feedback.Select(string.IsNullOrEmpty)
                                 .Select(x => !x)
                                 .ToReactiveProperty();

            Submit = HasContent.ToReactiveCommand()
                               .WithSubscribe(async () => await SendFeedback());
        }

        public ReactiveCommand Submit { get; set; }

        public ReactiveProperty<bool> HasContent { get; set; }

        public ReactiveProperty<string> Feedback { get; set; } = new ReactiveProperty<string>("");

        public async Task SendFeedback() => await QueuedTask.Run(async () => {
            var content = Feedback.Value;
            Feedback.Value = string.Empty;

            const string to = "sgourley@utah.gov";
            const string from = "no-reply@utah.gov";
            const string subject = "UIC Add-in Feedback";

            using (var message = new MailMessage(from, to, subject, content))
            using (var client = new SmtpClient("send.state.ut.us")) {
                client.Timeout = 100;
#if DEBUG
                client.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
                client.PickupDirectoryLocation = "c:/temp/";
#else
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
#endif
                var logFolder = UicModule.Current.GetAddinFolder();
                // 20180725
                var todaysLog = $"{DateTime.Today:yyyyMMdd}-log.txt";
                var fullPath = Path.Combine(logFolder, todaysLog);

                if (File.Exists(fullPath)) {
                    using (var fs = File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                        message.Attachments.Add(new Attachment(fs, todaysLog));

                        await client.SendMailAsync(message);
                    }
                } else {
                    await client.SendMailAsync(message);
                }
            }

            FrameworkApplication.AddNotification(new Notification {
                ImageUrl = "pack://application:,,,/ArcGIS.Desktop.Resources;component/Images/EmailUser32.png",
                Message = "Thank you for your feedback",
                Title = "Email Sent"
            });
        });
    }
}
