using System.Collections.Generic;

namespace ImageTools {
    class ImageMetadata {
        public object this[ImagePropertyId id] { get { return IdValueMapTable[id]; } set { IdValueMapTable[id] = value; } }
        Dictionary<ImagePropertyId, object> IdValueMapTable { get; } = new Dictionary<ImagePropertyId, object>();

        public bool TryGetValue<T>(ImagePropertyId id, out T value) {
            value = default(T);
            object result;
            if (IdValueMapTable.TryGetValue(id, out result)) {
                if (result != null && result is T)
                    value = (T)result;
                return true;
            }
            return false;
        }
    }
}