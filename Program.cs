using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MinimalApi.Dominio.Entidades;
using MinimalApi.Dominio.Enuns;
using MinimalApi.Dominio.Interfaces;
using MinimalApi.Dominio.ModelViews;
using MinimalApi.Dominio.Serviços;
using MinimalApi.DTOs;
using MinimalApi.Infraestrutura.DB;

#region Builder
var builder = WebApplication.CreateBuilder(args);
var key = builder.Configuration.GetSection("Jwt").ToString();
if (!string.IsNullOrEmpty(key)) key = "123456";


builder.Services.AddAuthentication(option =>
{
    option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(option =>
{
    option.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateLifetime = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))

    };
});

builder.Services.AddAuthorization();

builder.Services.AddScoped<IAdministradorServico, AdministradorServico>();
builder.Services.AddScoped<IVeiculoServico, VeiculoServico>();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<DbContexto>(options =>
{
    options.UseMySql(
        builder.Configuration.GetConnectionString("MySql"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("MySql"))
    );
});
var app = builder.Build();

#endregion

#region Home
app.MapGet("/", () => Results.Json(new Home { })).WithTags("Home");
#endregion

#region Administradores

string GerarTokenJwt(Administrador administrador)
{
    if (string.IsNullOrEmpty(key)) return string.Empty;

    // Garante que a chave tenha pelo menos 32 caracteres para ser segura
    var keySize = 32; // 256 bits (recomendado para HmacSha256)
    var secureKey = key.PadRight(keySize, '*');

    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secureKey));
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

    var claims = new List<Claim>
    {
        new Claim("Email", administrador.Email),
        new Claim("Perfil", administrador.Perfil)
    };

    var token = new JwtSecurityToken(
        claims: claims,
        expires: DateTime.Now.AddDays(1),
        signingCredentials: credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}


app.MapPost("administradores/login", ([FromBody] LoginDTO loginDTO, IAdministradorServico administradorServico) =>
{

    var adm = administradorServico.Login(loginDTO);

    if (adm != null)
    {
        string token = GerarTokenJwt(adm);
        return Results.Ok(new AdministradorLogado
        {
            Email = adm.Email,
            Perfil = adm.Perfil,
            Token = token
        });
    }
    else
        return Results.Unauthorized();

}).WithTags("Administrador");


app.MapGet("/administradores", ([FromQuery] int? pagina, IAdministradorServico administradorServico) =>
{
    var adms = new List<AdiministradorModelView>();
    var administradores = administradorServico.Todos(pagina);
    foreach (var adm in administradores)
    {
        adms.Add(new AdiministradorModelView
        {
            Id = adm.Id,
            Email = adm.Email,
            Perfil = adm.Perfil
        });
    }
    return Results.Ok(adms);

}).RequireAuthorization().WithTags("Administrador");


app.MapGet("/administradores/{id}", ([FromRoute] int id, IAdministradorServico administradorServico) =>
{
    var administrador = administradorServico.BuscaPorId(id);
    if (administrador == null) return Results.NotFound();

    return Results.Ok(new AdiministradorModelView
    {
        Id = administrador.Id,
        Email = administrador.Email,
        Perfil = administrador.Perfil

    });
}).RequireAuthorization().WithTags("Administrador");

app.MapPost("/administradores", ([FromBody] AdministradorDTO administradorDTO, IAdministradorServico administradorServico) =>
{
    var validacao = new ErroDeValidacao { Mensagem = new List<string>() };

    if (string.IsNullOrEmpty(administradorDTO.Email))
        validacao.Mensagem.Add("O email não pode ser vazio");

    if (string.IsNullOrEmpty(administradorDTO.Senha))
        validacao.Mensagem.Add("Sua senha não pode ser vazia");

    if (administradorDTO.Perfil == null)
        validacao.Mensagem.Add("Perfil não pode ser vazio");

    if (validacao.Mensagem.Count > 0)
        return Results.BadRequest(validacao);

    var administrador = new Administrador
    {
        Email = administradorDTO.Email,
        Senha = administradorDTO.Senha,
        Perfil = administradorDTO.Perfil.ToString() ?? Perfil.Editor.ToString()
    };

    administradorServico.Incluir(administrador);

    return Results.Created($"/administrador/{administrador.Id}", new AdiministradorModelView
    {
        Id = administrador.Id,
        Email = administrador.Email,
        Perfil = administrador.Perfil
    });

}).RequireAuthorization().WithTags("Administrador");

#endregion

#region Veiculos

ErroDeValidacao validaDTO(VeiculoDTO veiculoDTO)
{

    var validacao = new ErroDeValidacao
    {
        Mensagem = new List<string>()
    };

    if (string.IsNullOrEmpty(veiculoDTO.Nome))
        validacao.Mensagem.Add("O nome não pode ser vázio");

    if (string.IsNullOrEmpty(veiculoDTO.Marca))
        validacao.Mensagem.Add("A marca não pode ficar em branco");

    if (veiculoDTO.Ano < 1950)
        validacao.Mensagem.Add("Veiculo muito antigo, são aceitos apenas veículos do ano 1950 em diante");

    return validacao;

}

app.MapPost("/veiculos", ([FromBody] VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) =>
{

    var validacao = validaDTO(veiculoDTO);
    if (validacao.Mensagem.Count > 0)
        return Results.BadRequest(validacao);

    var veiculo = new Veiculo
    {
        Nome = veiculoDTO.Nome,
        Marca = veiculoDTO.Marca,
        Ano = veiculoDTO.Ano

    };
    veiculoServico.Incluir(veiculo);
    return Results.Created($"/veiculo/{veiculo.Id}", veiculo);
}).RequireAuthorization().WithTags("Veiculo");

app.MapGet("/veiculos", ([FromQuery] int? pagina, IVeiculoServico veiculoServico) =>
{

    var veiculos = veiculoServico.Todos(pagina);
    return Results.Ok(veiculos);
}).RequireAuthorization().WithTags("Veiculo");



app.MapGet("/veiculos/{id}", ([FromRoute] int id, IVeiculoServico veiculoServico) =>
{

    var veiculo = veiculoServico.BuscaPorId(id);
    if (veiculo == null) return Results.NotFound();

    return Results.Ok(veiculo);
}).RequireAuthorization().WithTags("Veiculo");

app.MapPut("/veiculos/{id}", ([FromRoute] int id, VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) =>
{

    var veiculo = veiculoServico.BuscaPorId(id);
    if (veiculo == null) return Results.NotFound();

    veiculo.Nome = veiculoDTO.Nome;
    veiculo.Marca = veiculoDTO.Marca;
    veiculo.Ano = veiculoDTO.Ano;

    veiculoServico.Atualizar(veiculo);

    return Results.Ok(veiculo);
}).RequireAuthorization().WithTags("Veiculo");

app.MapDelete("/veiculos/{id}", ([FromRoute] int id, IVeiculoServico veiculoServico) =>
{

    var veiculo = veiculoServico.BuscaPorId(id);
    if (veiculo == null) return Results.NotFound();

    veiculoServico.Apagar(veiculo);

    return Results.NoContent();
}).RequireAuthorization().WithTags("Veiculo");
#endregion

#region App
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.Run();
#endregion
