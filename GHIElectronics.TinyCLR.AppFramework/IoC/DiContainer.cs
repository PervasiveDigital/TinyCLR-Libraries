namespace GHIElectronics.TinyCLR.AppFramework.IoC {
    public static class DiContainer {
        static DiContainer() => Instance = new Container();

        public static Container Instance { get; private set; }
    }
}
