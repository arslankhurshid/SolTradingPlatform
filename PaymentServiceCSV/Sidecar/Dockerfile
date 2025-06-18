FROM mcr.microsoft.com/azure-cli

WORKDIR /app

COPY fetch-secret.sh .
RUN chmod +x fetch-secret.sh

CMD ["bash", "./fetch-secret.sh"]
