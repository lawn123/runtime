// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_compiler.h"
#include <stdint.h>
#include "opensslshim.h"

/*
Creates and initializes an EVP_MD_CTX with the given args.

Implemented by:
1) calling EVP_MD_CTX_create
2) calling EVP_DigestInit_ex on the new EVP_MD_CTX with the specified EVP_MD

Returns new EVP_MD_CTX on success, nullptr on failure.
*/
PALEXPORT EVP_MD_CTX* CryptoNative_EvpMdCtxCreate(const EVP_MD* type);

/*
Cleans up and deletes an EVP_MD_CTX instance created by EvpMdCtxCreate.

Implemented by:
1) Calling EVP_MD_CTX_destroy

No-op if ctx is null.
The given EVP_MD_CTX pointer is invalid after this call.
Always succeeds.
*/
PALEXPORT void CryptoNative_EvpMdCtxDestroy(EVP_MD_CTX* ctx);

/*
Resets an EVP_MD_CTX instance for a new computation.
*/
PALEXPORT int32_t CryptoNative_EvpDigestReset(EVP_MD_CTX* ctx, const EVP_MD* type);

/*
Function:
EvpDigestUpdate

Direct shim to EVP_DigestUpdate.
*/
PALEXPORT int32_t CryptoNative_EvpDigestUpdate(EVP_MD_CTX* ctx, const void* d, int32_t cnt);

/*
Function:
EvpDigestFinalEx

Direct shim to EVP_DigestFinal_ex.
*/
PALEXPORT int32_t CryptoNative_EvpDigestFinalEx(EVP_MD_CTX* ctx, uint8_t* md, uint32_t* s);

/*
Function:
EvpDigestFinalXOF

Direct shim to DigestFinalXOF.
*/
PALEXPORT int32_t CryptoNative_EvpDigestFinalXOF(EVP_MD_CTX* ctx, uint8_t* md, uint32_t len);

/*
Function:
EvpDigestCurrent

Shims EVP_DigestFinal_ex on a duplicated value of ctx.
*/
PALEXPORT int32_t CryptoNative_EvpDigestCurrent(const EVP_MD_CTX* ctx, uint8_t* md, uint32_t* s);

/*
Function:
EvpDigestCurrentXOF

Shims EVP_DigestFinalXOF on a duplicated value of ctx.
*/
PALEXPORT int32_t CryptoNative_EvpDigestCurrentXOF(const EVP_MD_CTX* ctx, uint8_t* md, uint32_t len);

/*
Function:
EvpDigestOneShot

Combines EVP_MD_CTX_create, EVP_DigestUpdate, and EVP_DigestFinal_ex in to a single operation.
*/
PALEXPORT int32_t CryptoNative_EvpDigestOneShot(const EVP_MD* type, const void* source, int32_t sourceSize, uint8_t* md, uint32_t* mdSize);

/*
Function:
EvpDigestXOFOneShot

Combines EVP_MD_CTX_create, EVP_DigestUpdate, and EVP_DigestFinalXOF in to a single operation.
*/
PALEXPORT int32_t CryptoNative_EvpDigestXOFOneShot(const EVP_MD* type, const void* source, int32_t sourceSize, uint8_t* md, uint32_t len);

/*
Function:
EvpMdCtxCopyEx

Creates a new EVP_MD_CTX and copies the ctx input using EVP_MD_CTX_copy_ex. Returns NULL on error.
*/
PALEXPORT EVP_MD_CTX* CryptoNative_EvpMdCtxCopyEx(const EVP_MD_CTX* ctx);

/*
Function:
EvpMdSize

Direct shim to EVP_MD_size.
*/
PALEXPORT int32_t CryptoNative_EvpMdSize(const EVP_MD* md);

/*
Function:
EvpDigestSqueeze

Calls EVP_DigestSqueeze. If the function is not available, haveFeature is set to zero and the return value should
be ignored. If the function is available, haveFeature is set to one and the operation result is returned.
*/
PALEXPORT int32_t CryptoNative_EvpDigestSqueeze(EVP_MD_CTX* ctx, uint8_t* md, uint32_t len, int32_t* haveFeature);

/*
Function:
EvpMd5

Direct shim to EVP_md5.
*/
PALEXPORT const EVP_MD* CryptoNative_EvpMd5(void);

/*
Function:
EvpSha1

Direct shim to EVP_sha1.
*/
PALEXPORT const EVP_MD* CryptoNative_EvpSha1(void);

/*
Function:
EvpSha256

Direct shim to EVP_sha256.
*/
PALEXPORT const EVP_MD* CryptoNative_EvpSha256(void);

/*
Function:
EvpSha384

Direct shim to EVP_sha384.
*/
PALEXPORT const EVP_MD* CryptoNative_EvpSha384(void);

/*
Function:
EvpSha512

Direct shim to EVP_sha512.
*/
PALEXPORT const EVP_MD* CryptoNative_EvpSha512(void);

/*
Function:
EvpSha3_256

Direct shim to EVP_sha3_256.
*/
PALEXPORT const EVP_MD* CryptoNative_EvpSha3_256(void);

/*
Function:
EvpSha3_384

Direct shim to EVP_sha3_384.
*/
PALEXPORT const EVP_MD* CryptoNative_EvpSha3_384(void);

/*
Function:
EvpSha3_512

Direct shim to EVP_sha3_512.
*/
PALEXPORT const EVP_MD* CryptoNative_EvpSha3_512(void);

/*
Function:
EvpShake128

Direct shim to EVP_shake128.
*/
PALEXPORT const EVP_MD* CryptoNative_EvpShake128(void);

/*
Function:
EvpShake256

Direct shim to EVP_shake256.
*/
PALEXPORT const EVP_MD* CryptoNative_EvpShake256(void);

/*
Function:
GetMaxMdSize

Returns the maximum bytes for a message digest.
*/
PALEXPORT int32_t CryptoNative_GetMaxMdSize(void);

/*
Filled the destination buffer with PBKDF2 derived data.

Implemented by:
1) Validating input
2) Calling PKCS5_PBKDF2_HMAC

password and salt may be NULL if their respective length parameters
are zero. When null, it will be replaced with a pointer to an empty
location.

Returns -1 on invalid input. On valid input, the return value
is the return value of PKCS5_PBKDF2_HMAC.
*/
PALEXPORT int32_t CryptoNative_Pbkdf2(const char* password,
                                      int32_t passwordLength,
                                      const unsigned char* salt,
                                      int32_t saltLength,
                                      int32_t iterations,
                                      const EVP_MD* digest,
                                      unsigned char* destination,
                                      int32_t destinationLength);
