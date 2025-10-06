using AutoMapper;
using CryptoDashboard.Domain.Entities;
using CryptoDashboard.Dto.Crypto;

namespace CryptoDashboard.Application.Mapping
{
    public class CryptoMappingProfile : Profile
    {
        public CryptoMappingProfile()
        {
            CreateMap<CryptoCurrency, CryptoDetailDto>();
            CreateMap<CryptoDetailDto, CryptoCurrency>();

            
        }
    }
}