namespace Dessentials.Common
{
    // Note: This singleton is used for classes that are not mono-targeted ones.
    // For mono-targeted ones, use MonoSingleton instead.
    // The differerence between the two is this singleton remains constantly through scenes,
    // therefore its data stays in memory all the time unless getting disposed properly.
    // In contrast with it, the MonoSingleton only exists in a scene, and once a new scene's loaded,
    // everything belongs to that MonoSingleton will be got rid.
    public class Singleton<T> where T : class, new()
    {
        #region Members

        private static T s_instance;

        #endregion Members

        #region Properties

        public static T Instance
        {
            get
            {
                if (s_instance == null)
                    s_instance = new T();
                return s_instance;
            }
        }

        #endregion Properties

        #region Class Methods

        protected virtual void Dispose()
        {
            s_instance = null;
        }

        #endregion Class Methods
    }
}