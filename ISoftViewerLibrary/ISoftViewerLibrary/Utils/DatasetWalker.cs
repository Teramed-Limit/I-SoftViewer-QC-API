using System.Threading.Tasks;
using Dicom;
using Dicom.IO.Buffer;
using Serilog;

namespace ISoftViewerLibrary.Utils
{
    public delegate void CollectItemDelegate(
        int level, DicomTag tag, string name, string vr, string length, string value);

    public class DatasetWalker : IDicomDatasetWalker
    {
        private readonly CollectItemDelegate _collectItem;
        private int _level;

        public DatasetWalker(CollectItemDelegate collectItem)
        {
            _collectItem = collectItem;
            Level = 0;
            SequenceEnd = false;
        }

        public DatasetWalker()
        {
            Level = 0;
            SequenceEnd = false;
        }


        public int Level
        {
            get => _level;
            set
            {
                _level = value;
                Indent = string.Empty;
                for (var i = 0; i < _level; i++)
                    Indent += "    ";
            }
        }

        private string Indent { get; set; }
        private bool SequenceEnd { get; set; }

        public bool OnElement(DicomElement element)
        {
            OnElementProcess(element);
            return true;
        }

        public Task<bool> OnElementAsync(DicomElement element)
        {
            OnElementProcess(element);
            return Task.FromResult(true);
        }

        private void OnElementProcess(DicomElement element)
        {
            if (SequenceEnd)
            {
                SequenceEnd = false;
                var sequenceDelimitationItem = $"{Indent}{DicomTag.SequenceDelimitationItem.ToString().ToUpper()}";
                _collectItem?.Invoke(Level, DicomTag.SequenceDelimitationItem,
                    DicomTag.SequenceDelimitationItem.DictionaryEntry.Name, string.Empty, string.Empty,
                    string.Empty);
            }

            var tag = $"{Indent}{element.Tag.ToString().ToUpper()}";
            var value = "<large value not displayed>";
            if (element.Length <= 2048)
                value = string.Join("\\", element.Get<string[]>());

            if (element.ValueRepresentation == DicomVR.UI && element.Count > 0)
            {
                var uid = element.Get<DicomUID>(0);
                var name = uid.Name;
                if (name != "Unknown")
                    value = $"{value} ({name})";
            }

            Log.Debug($"{tag} {element.Tag.DictionaryEntry.Name}: {value}");
            _collectItem?.Invoke(Level,
                element.Tag,
                element.Tag.DictionaryEntry.Name,
                element.ValueRepresentation.Code,
                element.Length.ToString(),
                value);
        }

        public bool OnBeginSequence(DicomSequence sequence)
        {
            SequenceEnd = false;
            var tag = $"{Indent}{sequence.Tag.ToString().ToUpper()}";
            Log.Debug($"{tag} {sequence.Tag.DictionaryEntry.Name}");
            _collectItem?.Invoke(Level,
                sequence.Tag,
                sequence.Tag.DictionaryEntry.Name,
                sequence.ValueRepresentation.Code,
                string.Empty,
                string.Empty);
            Level++;
            return true;
        }

        public bool OnBeginSequenceItem(DicomDataset dataset)
        {
            var tag = $"{Indent}{DicomTag.Item.ToString().ToUpper()}";
            // Log.Debug($"{tag} {DicomTag.Item.DictionaryEntry.Name}");
            _collectItem?.Invoke(Level, DicomTag.Item, DicomTag.Item.DictionaryEntry.Name, string.Empty, string.Empty,
                string.Empty);
            Level++;
            return true;
        }

        public bool OnEndSequenceItem()
        {
            Level--;
            return true;
        }

        public bool OnEndSequence()
        {
            SequenceEnd = true;
            var tag = $"{Indent}{DicomTag.ItemDelimitationItem.ToString().ToUpper()}";
            // Log.Debug($"{tag} {DicomTag.ItemDelimitationItem.DictionaryEntry.Name}");
            _collectItem?.Invoke(Level, DicomTag.ItemDelimitationItem,
                DicomTag.ItemDelimitationItem.DictionaryEntry.Name, string.Empty, string.Empty,
                string.Empty);
            Level--;
            return true;
        }

        public bool OnBeginFragment(DicomFragmentSequence fragment)
        {
            var tag = $"{Indent}{fragment.Tag.ToString().ToUpper()}";
            _collectItem?.Invoke(Level, fragment.Tag, fragment.Tag.DictionaryEntry.Name,
                fragment.ValueRepresentation.Code,
                string.Empty,
                string.Empty);
            Level++;
            return true;
        }

        public bool OnFragmentItem(IByteBuffer item)
        {
            var tag = $"{Indent}Fragment";
            _collectItem?.Invoke(Level, DicomTag.Item, DicomTag.Item.DictionaryEntry.Name, string.Empty,
                item.Size.ToString(), string.Empty);
            return true;
        }

        public bool OnEndFragment()
        {
            Level--;
            return true;
        }


        public Task<bool> OnFragmentItemAsync(IByteBuffer item)
        {
            return Task.FromResult(true);
        }


        public void OnEndWalk()
        {
        }

        public void OnBeginWalk()
        {
        }
    }
}