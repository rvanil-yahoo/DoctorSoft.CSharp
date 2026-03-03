namespace DoctorSoft.Domain.Contracts;

public interface ILegacyPasswordDecoder
{
    string Decode(string encoded);
}
