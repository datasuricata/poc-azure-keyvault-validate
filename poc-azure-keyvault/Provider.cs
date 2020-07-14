using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace poc_azure_keyvault
{
    public class Provider
    {
        private readonly Settings app;
        private readonly ILogger<Provider> log;

        public Provider(IOptions<Settings> options, ILogger<Provider> log)
        {
            this.app = options.Value;
            this.log = log;
        }

        //carregar blog e chave do keyvault
        public void Start()
        {
            try
            {
                log.LogInformation("iniciando procesos de criação da chave base64");

                log.LogInformation("recebendo parametros [accounteName] e [accountKey]");

                // This is standard code to interact with Blob storage.
                var creds = new StorageCredentials(app.AccountName, app.AccountKey);

                var account = new CloudStorageAccount(creds, useHttps: true);

                log.LogInformation($"credencial validada para a conta {app.AccountName}");

                log.LogInformation("criando cliente de conexão ao storage");

                var client = account.CreateCloudBlobClient();

                log.LogInformation("cliente conectado");

                log.LogInformation("localizando container de referencia");

                log.LogInformation("recebendo parametros [container]");

                var contain = client.GetContainerReference(app.Container);

                contain.CreateIfNotExists();

                log.LogInformation($"container localizado para {app.Container}");

                // The Resolver object is used to interact with Key Vault for Azure Storage.
                // This is where the GetToken method from above is used.
                var cloudResolver = new KeyVaultKeyResolver(GetToken);

                RetriveRSA(cloudResolver, contain);
            }
            catch (Exception e)
            {
                log.LogError(e, "falha ao processar informações da chave, verifique os dados e tente novamente");
            }
        }

        private void RetriveRSA(KeyVaultKeyResolver cloudResolver, CloudBlobContainer contain)
        {
            log.LogInformation($"validando chave externa de integração rsa");

            log.LogInformation("recebendo parametros [endpoint]");

            // Retrieve the key that you created previously.
            // The IKey that is returned here is an RsaKey.
            var rsa = cloudResolver.ResolveKeyAsync(app.Endpoint, CancellationToken.None)
                .GetAwaiter().GetResult();

            log.LogInformation($"chave rsa gerada com sucesso");
            log.LogInformation($"algoritimo {rsa.DefaultEncryptionAlgorithm}");
            log.LogInformation($"hash {rsa.DefaultKeyWrapAlgorithm}");
            log.LogInformation($"assinatura {rsa.DefaultSignatureAlgorithm}");

            // Now you simply use the RSA key to encrypt by setting it in the BlobEncryptionPolicy.
            var policy = new BlobEncryptionPolicy(rsa, null);
            var options = new BlobRequestOptions() { EncryptionPolicy = policy };

            log.LogInformation($"iniciando teste de upload no blob com autenticação segura");

            log.LogInformation($"validando referencia do blob para File.txt");
            
            // Reference a block blob.
            CloudBlockBlob blob = contain.GetBlockBlobReference("File.txt");

            log.LogInformation($"iniciando processo de upload");

            // Upload using the UploadFromStream method.
            using var stream = File.OpenRead(Directory.GetCurrentDirectory() + "\\File.txt");
            blob.UploadFromStream(stream, stream.Length, null, options, null);

            log.LogInformation($"valide se o arquivo File.txt é ");
        }

        //obtem o token do keyvault para o aplicativo de console
        private async Task<string> GetToken(string authority, string resource, string scope)
        {
            log.LogInformation($"resolvendo chave do keyvault");

            var authContext = new AuthenticationContext(authority);

            log.LogInformation($"criando credencial de acesso");

            log.LogInformation("recebendo parametros [clienteId] e [clienteSecret]");

            var clientCred = new ClientCredential(app.ClientId, app.ClientSecret);

            log.LogInformation($"validando ativação no servidor");

            var result = await authContext.AcquireTokenAsync(resource, clientCred);

            if (result == null)
            {
                log.LogError("falha ao validar credenciais de acesso");
                throw new InvalidOperationException("Failed to obtain the JWT token");
            }

            return result.AccessToken;
        }
    }
}
