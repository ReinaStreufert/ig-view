using LibChromeDotNet;
using LibChromeDotNet.ChromeInterop;
using LibChromeDotNet.HTML5;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ig_view
{
    public class MediaView : WebApp
    {
        public Attachment Src => _CurrentSrc;

        public MediaView(Attachment currentSrc, Action exitCallback) : base("mediaViewWeb/", "index.html")
        {
            _CurrentSrc = currentSrc;
            _ExitCallback = exitCallback;
        }

        private Attachment _CurrentSrc;
        private string? _LastSourceUrl;
        private IAppWindow? _Window;
        private Action _ExitCallback;

        public async Task SetAttachmentAsync(Attachment src)
        {
            _CurrentSrc = src;
            await UpdateContentAsync();
        }

        protected override async Task OnStartupAsync(IAppContext context)
        {
            var window = await context.OpenWindowAsync();
            window.ClosedByUser += _ExitCallback;
            _Window = window;
            await UpdateContentAsync();
        }

        private async Task UpdateContentAsync()
        {
            if (_Window == null)
                return;
            var docBody = await _Window.GetDocumentBodyAsync();
            var container = await docBody.QuerySelectAsync("#content");
            if (_LastSourceUrl != null)
                Content.RemoveSource(_LastSourceUrl);
            var mediaElementName = _CurrentSrc.Type switch
            {
                MessageAttachmentType.Photo => "img",
                MessageAttachmentType.Video => "video",
                MessageAttachmentType.Audio => "audio",
                _ => throw new NotImplementedException()
            };
            var outerHTML = await container.GetOuterHTMLAsync();
            var containerElementXml = outerHTML.DocumentElement!;
            for (int i = 0; i < containerElementXml.ChildNodes.Count; i++)
                containerElementXml.RemoveChild(containerElementXml.ChildNodes[i]!);
            var mediaElementXml = outerHTML.CreateElement(mediaElementName);
            var mediaSrcUuid = Guid.NewGuid();
            var srcPath = mediaSrcUuid.ToString("N");
            Content.AddFileSource(srcPath, _CurrentSrc.FilePath, _CurrentSrc.Type switch
            {
                MessageAttachmentType.Photo => "image/jpeg",
                MessageAttachmentType.Video => "video/mp4",
                MessageAttachmentType.Audio => "audio/mp4",
                _ => throw new NotImplementedException()
            });
            mediaElementXml.SetAttribute("src", srcPath);
            if (_CurrentSrc.Type == MessageAttachmentType.Video)
                mediaElementXml.SetAttribute("class", "video");
            if (_CurrentSrc.Type == MessageAttachmentType.Audio || _CurrentSrc.Type == MessageAttachmentType.Video)
                mediaElementXml.SetAttribute("controls", "true");
            containerElementXml.AppendChild(mediaElementXml);
            _LastSourceUrl = srcPath;
            await container.ModifyOuterHTMLAsync(outerHTML);
        }
    }
}
