namespace System {
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class STAThreadAttribute : Attribute {
        public STAThreadAttribute() {
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class MTAThreadAttribute : Attribute {
        public MTAThreadAttribute() {
        }
    }
}


