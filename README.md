_Work in progress_...

# Device Flow Authentication
 
## Start on device
```
POST https://service-device-auth-flow.azurewebsites.net/device_authorization
Accept: */*
Content-Type: application/x-www-form-urlencoded
Cache-Control: no-cache

clientId=mydeviceId
``` 

Example Response:
```
{
    "device_code":"9ab010de-9fe7-4f62-96c9-e9498004211e",
    "user_code":"211313","verification_uri":"https://service-device-auth-flow.azurewebsites.net/",
    "expires_in":300
}

```
## Navigate to website

https://service-device-auth-flow.azurewebsites.net/

Enter User Code and login

### Poll for token on device
```
POST https://service-device-auth-flow.azurewebsites.net/token
Accept: */*
Content-Type: application/x-www-form-urlencoded
Cache-Control: no-cache

grant_type=urn:ietf:params:oauth:grant-type:device_code&client_Id=mydeviceId&device_code=9ab010de-9fe7-4f62-96c9-e9498004211e
```

Pending example response:
```
HTTP/1.1 400 Bad Request
Content-Length: 33
Content-Type: application/json; charset=utf-8
Set-Cookie: ARRAffinity=dd716a6def04e48f4e433f7740cecb7f8a4f1c77d318c5480b769fc5157ad936;Path=/;HttpOnly;Domain=service-device-auth-flow.azurewebsites.net
Date: Wed, 30 Sep 2020 12:37:11 GMT

{
  "value": "authorization_pending"
}
```
Expired token
```
HTTP/1.1 400 Bad Request
Content-Length: 13
Content-Type: text/plain; charset=utf-8
Set-Cookie: ARRAffinity=dd716a6def04e48f4e433f7740cecb7f8a4f1c77d318c5480b769fc5157ad936;Path=/;HttpOnly;Domain=service-device-auth-flow.azurewebsites.net
Date: Wed, 30 Sep 2020 12:44:33 GMT

expired_token
```

Token

```
HTTP/1.1 200 OK
Content-Type: text/plain; charset=utf-8
Vary: Accept-Encoding
Set-Cookie: ARRAffinity=dd716a6def04e48f4e433f7740cecb7f8a4f1c77d318c5480b769fc5157ad936;Path=/;HttpOnly;Domain=service-device-auth-flow.azurewebsites.net
Date: Wed, 30 Sep 2020 12:46:31 GMT

{
    "access_Token":"eyJraWQiOiJ2VE96SmhwS3dIeD....",
    "token_type":null,
    "expires_in":0,
    "refresh_token":null,
    "scope":null}

```