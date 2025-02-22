using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MinimalApi.Dominio.Entidades;

namespace MinimalApi.Infraestrutura.DB
{
    public class DbContexto : DbContext
    {

        private readonly IConfiguration _configuracaoAppSettings;
        public DbContexto(IConfiguration configuracaoAppSettings)
        {
            _configuracaoAppSettings = configuracaoAppSettings;
        }


        public DbSet<Administrador> Administradores { get; set; } = default!;
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        if(!optionsBuilder.IsConfigured){
            var stringConexao = _configuracaoAppSettings.GetConnectionString("mysql").ToString();
            if (!string.IsNullOrEmpty(stringConexao))
            {

                optionsBuilder.UseMySql(
                    stringConexao,
                ServerVersion.AutoDetect(stringConexao)
                );
            }
        }
    }
}
}