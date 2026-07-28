using System;
using System.Globalization;
using SaiGame.Services;
using UnityEngine.UIElements;

namespace SG03.UI.Components
{
    public sealed class ServerTimeLabelComponent : IDisposable
    {
        public const float DefaultWidth = 150f;

        private readonly VisualElement root;
        private readonly Label dateLabel;
        private readonly Label[] timeDigitLabels;
        private IVisualElementScheduledItem updateSchedule;

        public ServerTimeLabelComponent(VisualElement root, float fixedWidth = DefaultWidth)
        {
            this.root = root ?? throw new ArgumentNullException(nameof(root));
            this.ConfigureFixedLayout(fixedWidth);
            this.dateLabel = this.CreateLabel("Date", 48f);
            this.root.Add(this.dateLabel);
            this.root.Add(this.CreateLabel("DateTimeSeparator", 10f, "|"));
            this.timeDigitLabels = new[]
            {
                this.CreateLabel("HourTens", 10f),
                this.CreateLabel("HourOnes", 10f),
                this.CreateLabel("MinuteTens", 10f),
                this.CreateLabel("MinuteOnes", 10f),
                this.CreateLabel("SecondTens", 10f),
                this.CreateLabel("SecondOnes", 10f),
            };
            this.root.Add(this.timeDigitLabels[0]);
            this.root.Add(this.timeDigitLabels[1]);
            this.root.Add(this.CreateLabel("HourMinuteSeparator", 5f, ":"));
            this.root.Add(this.timeDigitLabels[2]);
            this.root.Add(this.timeDigitLabels[3]);
            this.root.Add(this.CreateLabel("MinuteSecondSeparator", 5f, ":"));
            this.root.Add(this.timeDigitLabels[4]);
            this.root.Add(this.timeDigitLabels[5]);
            this.root.RegisterCallback<AttachToPanelEvent>(this.OnAttachToPanel);
            this.root.RegisterCallback<DetachFromPanelEvent>(this.OnDetachFromPanel);
            this.UpdateText();
            this.StartUpdates();
        }

        public void Dispose()
        {
            this.root.UnregisterCallback<AttachToPanelEvent>(this.OnAttachToPanel);
            this.root.UnregisterCallback<DetachFromPanelEvent>(this.OnDetachFromPanel);
            this.updateSchedule?.Pause();
        }

        private void OnAttachToPanel(AttachToPanelEvent _)
        {
            this.UpdateText();
            this.StartUpdates();
        }

        private void OnDetachFromPanel(DetachFromPanelEvent _)
        {
            this.updateSchedule?.Pause();
        }

        private void StartUpdates()
        {
            if (this.updateSchedule == null)
                this.updateSchedule = this.root.schedule.Execute(this.UpdateText).Every(1000);
            else
                this.updateSchedule.Resume();
        }

        private void ConfigureFixedLayout(float fixedWidth)
        {
            this.root.style.width = fixedWidth;
            this.root.style.minWidth = fixedWidth;
            this.root.style.maxWidth = fixedWidth;
            this.root.style.flexGrow = 0;
            this.root.style.flexShrink = 0;
            this.root.style.flexDirection = FlexDirection.Row;
            this.root.style.justifyContent = Justify.FlexEnd;
            this.root.style.alignItems = Align.Center;
        }

        private Label CreateLabel(string suffix, float width, string text = "")
        {
            Label label = new Label(text) { name = $"{this.root.name}{suffix}" };
            label.style.width = width;
            label.style.minWidth = width;
            label.style.maxWidth = width;
            label.style.flexGrow = 0;
            label.style.flexShrink = 0;
            label.style.marginLeft = 0;
            label.style.marginTop = 0;
            label.style.marginRight = 0;
            label.style.marginBottom = 0;
            label.style.paddingLeft = 0;
            label.style.paddingTop = 0;
            label.style.paddingRight = 0;
            label.style.paddingBottom = 0;
            label.style.unityTextAlign = UnityEngine.TextAnchor.MiddleCenter;
            return label;
        }

        private void UpdateText()
        {
            SaiServer server = SaiServer.Instance;
            if (server == null || !server.HasServerTime)
            {
                this.dateLabel.text = "--- --";
                this.SetTimeDigits("------");
                return;
            }

            DateTime currentTime = server.CurrentServerTime;
            this.dateLabel.text = currentTime.ToString("MMM dd", CultureInfo.InvariantCulture);
            this.SetTimeDigits(currentTime.ToString("HHmmss", CultureInfo.InvariantCulture));
        }

        private void SetTimeDigits(string value)
        {
            for (int index = 0; index < this.timeDigitLabels.Length; index++)
                this.timeDigitLabels[index].text = value[index].ToString();
        }
    }
}
