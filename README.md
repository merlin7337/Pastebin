This project is my realization of a [Pastebin system](https://en.wikipedia.org/wiki/Pastebin) which is quite simple. It's an ASP.NET web api with following technologies used:
+ PostgreSQL
+ AWS integration
+ Docker
+ Kubernetes

I was really into making an [AutoDeletionService.cs](https://github.com/merlin7337/Pastebin/blob/main/Pastebin/Services/AutoDeletionService.cs) which is background service that deletes expired pastes metadata from SQL database and paste's content from AWS cloud every minute

### System design:
![Pastebin](https://github.com/merlin7337/Pastebin/assets/112899660/e23af716-2514-4d1e-bd88-aaed2b7f9c9e)

