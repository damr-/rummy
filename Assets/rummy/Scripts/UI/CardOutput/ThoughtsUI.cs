namespace rummy.UI.CardOutput
{

    public class ThoughtsUI : CardOutputUI
    {
        protected override void SetupPlayerSub()
        {
            player.NewThought.AddListener(UpdateThoughts);
        }

        private void UpdateThoughts(string newThought)
        {
            if (!gameObject.activeInHierarchy)
                return;

            if (newThought == "<CLEAR>")
                outputView.ClearMessages();
            else
                outputView.PrintMessage(new ScrollView.Message(newThought));
        }
    }

}