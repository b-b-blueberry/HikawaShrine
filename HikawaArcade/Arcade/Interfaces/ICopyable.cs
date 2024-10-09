namespace HikawaArcade.Arcade.Interfaces
{
	public interface ICopyable
	{
		public void Reset() {}

		public ICopyable CopyTo(ICopyable target)
		{
			target.Reset();
			return target;
		}
	}
}
