namespace Dessentials.Utility
{
    /// <summary>
    /// Something that owns Addressable content it can load ahead of use and give back. Lets a loading screen,
    /// a scene's setup or a pool warm up and release a mixed set of such things without knowing what each one is.
    /// </summary>
    public interface IAddressablePreloadable
    {
        /// <summary>
        /// Starts loading the content so a later use has nothing left to wait for. Does nothing when it is
        /// already loaded or on its way, and returns at once: the load finishes in the background.
        /// </summary>
        void Preload();

        /// <summary>
        /// Releases what <see cref="Preload"/> (or first use) loaded. A load still running is dropped when it
        /// lands. Safe to call when nothing is loaded, and a later <see cref="Preload"/> loads again.
        /// </summary>
        void Unload();
    }
}
