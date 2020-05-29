using rummy.Cards;

namespace rummy.Utility
{

    public class Tb : Singleton<Tb>
    {
        protected Tb() { } // guarantee this will be always a singleton only - can't use the constructor!

        public GameMaster GameMaster;
        public CardStack CardStack;
        public DiscardStack DiscardStack;
    }

}