namespace Markov.Services.Interfaces;

  public interface IIndicatorProvider
    {
        decimal[] GetSma(int period);
        decimal[] GetVolumeSma(int period);
        decimal[] GetRsi(int period);
        decimal[] GetAtr(int period);
    }