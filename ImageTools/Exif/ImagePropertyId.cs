namespace ImageTools {
    public class ImagePropertyId {
        public static ImagePropertyId FileChangeDateTime = new ImagePropertyId(306);
        public static ImagePropertyId DateTimeOriginal = new ImagePropertyId(36867);
        public static ImagePropertyId DateTimeDigitized = new ImagePropertyId(36868);

        readonly int id;
        private ImagePropertyId(int id) {
            this.id = id;
        }

        public override bool Equals(object obj) {
            return (obj as ImagePropertyId)?.id == id;
        }
        public override int GetHashCode() {
            return id;
        }
        public override string ToString() {
            return id.ToString();
        }
        public static explicit operator int(ImagePropertyId prop) {
            return prop.id;
        }
        public static explicit operator ImagePropertyId(int id) {
            return new ImagePropertyId(id);
        }
    }
}