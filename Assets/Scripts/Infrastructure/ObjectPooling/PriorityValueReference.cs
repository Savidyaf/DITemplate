namespace SpiralingStudio.Utils
{
    public class PriorityValueReference
    {
        private int overridePriority;

        public PriorityValueReference(int priority)
        {
            overridePriority = priority;
        }

        public int GetPriority()
        {
            return overridePriority;
        }

        public void UpdatePriority(int newPriority)
        {
            overridePriority = newPriority;
        }
    }
}