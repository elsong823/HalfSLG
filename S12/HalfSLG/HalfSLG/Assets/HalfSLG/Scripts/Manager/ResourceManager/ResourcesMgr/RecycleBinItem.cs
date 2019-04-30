namespace ELGame.Resource
{
    public class RecycleBinItem
        : IRecyclable
    {
        private static LitePool<RecycleBinItem> pool = new LitePool<RecycleBinItem>();
        public static RecycleBinItem Get()
        {
            var item = pool.Get();
            return item;
        }

        public AssetBundleInfoNode assetBundleInfoNode;
        public float timeStamp;

        public void Clear()
        {
            if (assetBundleInfoNode != null)
            {
                assetBundleInfoNode.DoUnload();

                pool.Return(this);
            }
        }

        public void OnRecycle()
        {
            assetBundleInfoNode = null;
            timeStamp = 0;
        }

        public AssetBundleInfoNode Cancel()
        {
            AssetBundleInfoNode node = assetBundleInfoNode;
            pool.Return(this);
            return node;
        }

        //TODO:
        public int Weight
        {
            get
            {
                return 0;
            }
        }
    }
}