using UniRx;

namespace romme.UI.CardOutput
{

    public class ThoughtsUI : CardOutputUI
    {
        protected override void SetupPlayerSub()
        {
            player.NewThought.Subscribe(UpdateThoughts);
        }

        private void UpdateThoughts(string newThought)
        {
            if (newThought == "<CLEAR>")
                outputView.ClearMessages();
            else
                outputView.PrintMessage(new ScrollView.Message(newThought));
        }
    }

}