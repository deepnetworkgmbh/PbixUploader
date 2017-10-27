Power BI Report (pbix) Uploader
===================

# About

**PbixUploader** command line tool creates the given **workspace** (if it doesn't exist before) and uploads the **pbix file** into **POWER BI** account provided. 

# How to use PbixUploader

 **(1) Get a ClientID for your application :**
 
 Complete the steps provided in the following link and register your application to get a ClientID for it.
 
 https://powerbi.microsoft.com/en-us/documentation/powerbi-developer-register-app/#register-with-the-power-bi-app-registration-tool
 
 
**(2) Run PbixUploader with the required arguments :**
 
 You need to enter following arguments along with exe file :
 
*  **Client ID :** `-c`  or   `--clientid`

*  **Workspace Name :** `-w`  or  `--workspace`

*  **Username of Power BI account :** `-u` or `--uname`

*  **Password of Power BI account :** `-p` `--passwd`    

If everything goes OK, you should see a similar message as below :

> Upload process completed with success: {"id":"dda10e34-cf5c-4ca8-8cd8-ec1025866234"}



