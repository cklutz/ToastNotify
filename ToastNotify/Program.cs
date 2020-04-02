using System;
using System.Text;
using System.Threading;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace ToastNotify
{
    public static class Program
    {
        private static int Usage()
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine("Usage: {0} [OPTIONS] MESSAGE", typeof(Program).Assembly.GetName().Name);
            Console.Error.WriteLine();
            Console.Error.WriteLine("Produce a toast notification with MESSAGE.");
            Console.Error.WriteLine();
            Console.Error.WriteLine("Options:");
            Console.Error.WriteLine("-title:STR         Set the notification title to STR.");
            Console.Error.WriteLine("-image:FILE        Set the notification image from FILE.");
            Console.Error.WriteLine("-alarm             Set the notification scenario to alarm.");
            Console.Error.WriteLine("-reminder          Set the notification scenario to reminder.");
            Console.Error.WriteLine("-verbose           Enable verbose output.");
            Console.Error.WriteLine();
            Console.Error.WriteLine("Advanced Options:");
            Console.Error.WriteLine("-wait:MILLISECONDS Wait MILLISECONDS after issuing notification.");
            Console.Error.WriteLine("                   Might be required for notification to show.");
            Console.Error.WriteLine("-appId:APPID       Issue notification with APPID. The default APPID");
            Console.Error.WriteLine("                   being used, is that of Windows PowerShell.");
            Console.Error.WriteLine();

            return 1;
        }

        public static int Main(string[] args)
        {
            try
            {
                // See https://stackoverflow.com/a/46817674/21567.
                string appId = @"{1AC14E77-02E7-4E5D-B744-2EB1AE5198B7}\WindowsPowerShell\v1.0\powershell.exe";
                string title = "Notification";
                string image = null;
                string scenario = null;
                bool verbose = false;

                // ToastNotificationManager.Show() is async. If we exit this application before the toast has actually been
                // shown to the user, it never will. On option is to wait for some time, however how long is not clear.
                // However, simply, non representative tests have shown, that even 10 ms seems to be sufficient.
                // Obviously, that value would depend on lots of external factors (system load, etc.). So we
                // default to 500ms. That is still barely visible to the casual user.
                int waitMilliseconds = 500;

                int i = 0;
                for (; i < args.Length; i++)
                {
                    string arg = args[i];
                    if (arg.Equals("-h", StringComparison.OrdinalIgnoreCase) ||
                        arg.Equals("-help", StringComparison.OrdinalIgnoreCase))
                    {
                        return Usage();
                    }
                    else if (arg.StartsWith("-appId:", StringComparison.OrdinalIgnoreCase))
                    {
                        appId = arg.Substring("-appId:".Length);
                    }
                    else if (arg.Equals("-alarm", StringComparison.OrdinalIgnoreCase))
                    {
                        scenario = "alarm";
                    }
                    else if (arg.Equals("-reminder", StringComparison.OrdinalIgnoreCase))
                    {
                        scenario = "reminder";
                    }
                    else if (arg.Equals("-verbose", StringComparison.OrdinalIgnoreCase))
                    {
                        verbose = true;
                    }
                    else if (arg.StartsWith("-image:", StringComparison.OrdinalIgnoreCase))
                    {
                        image = arg.Substring("-image:".Length);
                    }
                    else if (arg.StartsWith("-title:", StringComparison.OrdinalIgnoreCase))
                    {
                        title = arg.Substring("-title:".Length);
                    }
                    else if (arg.StartsWith("-wait:", StringComparison.OrdinalIgnoreCase))
                    {
                        waitMilliseconds = int.Parse(arg.Substring("-wait:".Length));
                    }
                    else if (arg == "--" || !arg.StartsWith("-"))
                    {
                        break;
                    }
                    else
                    {
                        Console.Error.WriteLine("error: unknown option {0}", arg);
                        return Usage();
                    }
                }

                if (i >= args.Length)
                {
                    Console.Error.WriteLine("error: missing message");
                    return Usage();
                }

                var sb = new StringBuilder();
                for (; i < args.Length; i++)
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(" ");
                    }

                    sb.Append(args[i]);
                }

                ShowImageToast(waitMilliseconds, verbose, appId, title, sb.ToString(), image, scenario);

            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("error: " + ex);
                return ex.HResult;
            }
            return 0;
        }

        private static void ShowImageToast(int waitMilliseconds, bool verbose, string appId, string title, string message, string image, string scenario)
        {
            XmlDocument toastXml;

            if (!string.IsNullOrEmpty(image))
            {
                toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText02);
            }
            else
            {
                toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
            }

            // Fill in the text elements
            var textElements = toastXml.GetElementsByTagName("text");
            textElements[0].AppendChild(toastXml.CreateTextNode(title));
            if (message != null)
            {
                textElements[1].AppendChild(toastXml.CreateTextNode(message));
            }

            if (!string.IsNullOrEmpty(image))
            {
                // Specify the absolute path to an image
                string imagePath = "file:///" + image;
                var imageElements = toastXml.GetElementsByTagName("image");
                imageElements[0].Attributes.GetNamedItem("src").NodeValue = imagePath;
            }

            if (!string.IsNullOrEmpty(scenario))
            {
                var toastElement = toastXml.GetElementsByTagName("toast");
                var scenarioAttribute = toastXml.CreateAttribute("scenario");
                scenarioAttribute.Value = scenario.ToLowerInvariant();
                toastElement[0].Attributes.SetNamedItem(scenarioAttribute);
            }

            if (verbose)
            {
                Console.WriteLine("---------------- TOAST_BEGIN ----------------");
                Console.WriteLine(toastXml.GetXml());
                Console.WriteLine("---------------- TOAST_END ----------------");
                Console.WriteLine("Using App ID: {0}", appId);
            }

            var toast = new ToastNotification(toastXml);
            toast.Failed += (s, e) =>
            {
                Console.WriteLine("error: notification failed: {0}", e.ErrorCode);
            };

            var notifier = ToastNotificationManager.CreateToastNotifier(appId);

            notifier.Show(toast);

            if (waitMilliseconds > 0)
            {
                Thread.Sleep(waitMilliseconds);
            }
        }
    }
}
