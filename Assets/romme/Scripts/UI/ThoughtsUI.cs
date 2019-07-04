using UniRx;

namespace romme.UI
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
                ClearThoughts();
            else
                outputView.PrintMessage(new ScrollView.Message(newThought));
        }

        private void ClearThoughts()
        {
            outputView.ClearMessages();
        }
    }

}