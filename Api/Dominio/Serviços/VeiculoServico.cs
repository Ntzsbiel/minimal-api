using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MinimalApi.Dominio.Entidades;
using MinimalApi.Dominio.Interfaces;
using MinimalApi.DTOs;
using MinimalApi.Infraestrutura.DB;

namespace MinimalApi.Dominio.Serviços
{
    public class VeiculoServico : IVeiculoServico
    {
        private readonly DbContexto _contexto;
        public VeiculoServico(DbContexto contexto)
        {
            _contexto = contexto;
        }
        void IVeiculoServico.Apagar(Veiculo veiculo)
        {
            _contexto.Veiculos.Remove(veiculo);
            _contexto.SaveChanges();
        }

        public Veiculo? BuscaPorId(int id)
        {
            return _contexto.Veiculos.Where(v => v.Id == id).FirstOrDefault();
        }

        void IVeiculoServico.Atualizar(Veiculo veiculo)
        {
            _contexto.Veiculos.Update(veiculo);
            _contexto.SaveChanges();
        }

        void IVeiculoServico.Incluir(Veiculo veiculo)
        {
            _contexto.Veiculos.Add(veiculo);
            _contexto.SaveChanges();
        }
        public List<Veiculo> Todos(int? pagina = 1, string nome = null, string marca = null)
        {
            var query = _contexto.Veiculos.AsQueryable();
            if(!string.IsNullOrEmpty(nome)){
                query = query.Where(v => EF.Functions.Like(v.Nome.ToLower(),$"%{nome.ToLower()}%"));
            }

            int itensPorPagina = 10;

            if (pagina != null){ 
            query = query.Skip(((int)pagina - 1)* itensPorPagina).Take(itensPorPagina);
            }
            
            return query.ToList();
        }
    }
}
