S3ContentEncoder
================

Updates all objects with a prefix in an S3 bucket to use the specified encoding (e.g. gzip/deflate) and set the Content-Encoding HTTP header accordingly.

Usage
-----

Get help:

	> S3ContentEncoder.exe --help 
	S3ContentEncoder 1.0.0.0                                                                                
	Copyright Scott Lerch 2013                                                                            
                                                                                                        
	  -a, --accessKey    Required.                                                                          
                                                                                                        
	  -s, --secretKey    Required.                                                                          
                                                                                                        
	  -b, --bucket       Required.                                                                          
                                                                                                        
	  -p, --prefix       Required.                                                                          
                                                                                                        
	  -e, --encoding     (Default: gzip)                                                                    
                                                                                                        
	  --help             Display this help screen.`                                               

Run:

	> S3ContentEncoder.exe --accessKey=XYZ --secretKey=XYZ --bucket=mybucket --prefix=some/directory --encoding=gzip

Limitations
-----------

- Only handles gzip
- Does not preserve ACLs, metadata, or other custom headers
