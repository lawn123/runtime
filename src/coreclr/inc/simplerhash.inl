// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _SIMPLERHASHTABLE_INL_
#define _SIMPLERHASHTABLE_INL_

// To implement magic-number divide with a 32-bit magic number,
// multiply by the magic number, take the top 64 bits, and shift that
// by the amount given in the table.

inline
unsigned magicNumberDivide(unsigned numerator, const PrimeInfo &p)
{
    uint64_t num = numerator;
    uint64_t mag = p.magic;
    uint64_t product = (num * mag) >> (32 + p.shift);
    return (unsigned) product;
}

inline
unsigned magicNumberRem(unsigned numerator, const PrimeInfo &p)
{
    unsigned div = magicNumberDivide(numerator, p);
    unsigned result = numerator - (div * p.prime);
    assert(result == numerator % p.prime);
    return result;
}

template <typename Key, typename KeyFuncs, typename Value, typename Behavior>
SimplerHashTable<Key,KeyFuncs,Value,Behavior>::SimplerHashTable(IAllocator* alloc)
  : m_alloc(alloc),
    m_table(NULL),
    m_tableSizeInfo(),
    m_tableCount(0),
    m_tableMax(0)
{
    assert(m_alloc != nullptr);

#ifndef __GNUC__ // these crash GCC
    static_assert_no_msg(Behavior::s_growth_factor_numerator > Behavior::s_growth_factor_denominator);
    static_assert_no_msg(Behavior::s_density_factor_numerator < Behavior::s_density_factor_denominator);
#endif
}

template <typename Key, typename KeyFuncs, typename Value, typename Behavior>
SimplerHashTable<Key,KeyFuncs,Value,Behavior>::~SimplerHashTable()
{
    RemoveAll();
}

template <typename Key, typename KeyFuncs, typename Value, typename Behavior>
void * SimplerHashTable<Key,KeyFuncs,Value,Behavior>::operator new(size_t sz, IAllocator * alloc)
{
    return alloc->Alloc(sz);
}

template <typename Key, typename KeyFuncs, typename Value, typename Behavior>
void * SimplerHashTable<Key,KeyFuncs,Value,Behavior>::operator new[](size_t sz, IAllocator * alloc)
{
    return alloc->Alloc(sz);
}

template <typename Key, typename KeyFuncs, typename Value, typename Behavior>
void SimplerHashTable<Key,KeyFuncs,Value,Behavior>::operator delete(void * p, IAllocator * alloc)
{
    alloc->Free(p);
}

template <typename Key, typename KeyFuncs, typename Value, typename Behavior>
void SimplerHashTable<Key,KeyFuncs,Value,Behavior>::operator delete[](void * p, IAllocator * alloc)
{
    alloc->Free(p);
}

template <typename Key, typename KeyFuncs, typename Value, typename Behavior>
unsigned SimplerHashTable<Key,KeyFuncs,Value,Behavior>::GetCount() const
{
    return m_tableCount;
}

template <typename Key, typename KeyFuncs, typename Value, typename Behavior>
bool SimplerHashTable<Key,KeyFuncs,Value,Behavior>::Lookup(Key key, Value* pVal) const
{
    Node* pN = FindNode(key);

    if (pN != NULL)
    {
        if (pVal != NULL)
        {
            *pVal = pN->m_val;
        }
        return true;
    }
    else
    {
        return false;
    }
}

template <typename Key, typename KeyFuncs, typename Value, typename Behavior>
Value *SimplerHashTable<Key,KeyFuncs,Value,Behavior>::LookupPointer(Key key) const
{
    Node* pN = FindNode(key);

    if (pN != NULL)
        return &(pN->m_val);
    else
        return NULL;
}

template <typename Key, typename KeyFuncs, typename Value, typename Behavior>
typename SimplerHashTable<Key,KeyFuncs,Value,Behavior>::Node*
SimplerHashTable<Key,KeyFuncs,Value,Behavior>::FindNode(Key k) const
{
    if (m_tableSizeInfo.prime == 0)
        return NULL;

    unsigned index = GetIndexForKey(k);

    Node* pN = m_table[index];
    if (pN == NULL)
        return NULL;

    // Otherwise...
    while (pN != NULL && !KeyFuncs::Equals(k, pN->m_key))
        pN = pN->m_next;

    assert(pN == NULL || KeyFuncs::Equals(k, pN->m_key));

    // If pN != NULL, it's the node for the key, else the key isn't mapped.
    return pN;
}

template <typename Key, typename KeyFuncs, typename Value, typename Behavior>
unsigned SimplerHashTable<Key,KeyFuncs,Value,Behavior>::GetIndexForKey(Key k) const
{
    unsigned hash = KeyFuncs::GetHashCode(k);

    unsigned index = magicNumberRem(hash, m_tableSizeInfo);

    return index;
}

template <typename Key, typename KeyFuncs, typename Value, typename Behavior>
bool SimplerHashTable<Key,KeyFuncs,Value,Behavior>::Set(Key k, Value v)
{
    CheckGrowth();

    assert(m_tableSizeInfo.prime != 0);

    unsigned index = GetIndexForKey(k);

    Node* pN = m_table[index];
    while (pN != NULL && !KeyFuncs::Equals(k, pN->m_key))
    {
        pN = pN->m_next;
    }
    if (pN != NULL)
    {
        pN->m_val = v;
        return true;
    }
    else
    {
        Node* pNewNode = new (m_alloc) Node(k, v, m_table[index]);
        m_table[index] = pNewNode;
        m_tableCount++;
        return false;
    }
}

template <typename Key, typename KeyFuncs, typename Value, typename Behavior>
bool SimplerHashTable<Key,KeyFuncs,Value,Behavior>::Remove(Key k)
{
    unsigned index = GetIndexForKey(k);

    Node* pN = m_table[index];
    Node** ppN = &m_table[index];
    while (pN != NULL && !KeyFuncs::Equals(k, pN->m_key))
    {
        ppN = &pN->m_next;
        pN = pN->m_next;
    }
    if (pN != NULL)
    {
        *ppN = pN->m_next;
        m_tableCount--;
        Node::operator delete(pN, m_alloc);
        return true;
    }
    else
    {
        return false;
    }
}

template <typename Key, typename KeyFuncs, typename Value, typename Behavior>
void SimplerHashTable<Key,KeyFuncs,Value,Behavior>::RemoveAll()
{
    for (unsigned i = 0; i < m_tableSizeInfo.prime; i++)
    {
        for (Node* pN = m_table[i]; pN != NULL; )
        {
            Node* pNext = pN->m_next;
            Node::operator delete(pN, m_alloc);
            pN = pNext;
        }
    }
    m_alloc->Free(m_table);

    m_table = NULL;
    m_tableSizeInfo = PrimeInfo();
    m_tableCount = 0;
    m_tableMax = 0;

    return;
}

template <typename Key, typename KeyFuncs, typename Value, typename Behavior>
typename SimplerHashTable<Key,KeyFuncs,Value,Behavior>::KeyIterator SimplerHashTable<Key,KeyFuncs,Value,Behavior>::Begin() const
{
    KeyIterator i(this, TRUE);
    return i;
}

template <typename Key, typename KeyFuncs, typename Value, typename Behavior>
typename SimplerHashTable<Key,KeyFuncs,Value,Behavior>::KeyIterator SimplerHashTable<Key,KeyFuncs,Value,Behavior>::End() const
{
    return KeyIterator(this, FALSE);
}

template <typename Key, typename KeyFuncs, typename Value, typename Behavior>
void SimplerHashTable<Key,KeyFuncs,Value,Behavior>::CheckGrowth()
{
    if (m_tableCount == m_tableMax)
    {
        Grow();
    }
}

template <typename Key, typename KeyFuncs, typename Value, typename Behavior>
void SimplerHashTable<Key,KeyFuncs,Value,Behavior>::Grow()
{
    unsigned newSize = (unsigned) (m_tableCount
                                   * Behavior::s_growth_factor_numerator / Behavior::s_growth_factor_denominator
                                   * Behavior::s_density_factor_denominator / Behavior::s_density_factor_numerator);
    if (newSize < Behavior::s_minimum_allocation)
        newSize = Behavior::s_minimum_allocation;

    // handle potential overflow
    if (newSize < m_tableCount)
        Behavior::NoMemory();

    Reallocate(newSize);
}

template <typename Key, typename KeyFuncs, typename Value, typename Behavior>
void SimplerHashTable<Key,KeyFuncs,Value,Behavior>::Reallocate(unsigned newTableSize)
{
    assert(newTableSize >= (GetCount() * Behavior::s_density_factor_denominator / Behavior::s_density_factor_numerator));

    // Allocation size must be a prime number.  This is necessary so that hashes uniformly
    // distribute to all indices, and so that chaining will visit all indices in the hash table.
    PrimeInfo newPrime = NextPrime(newTableSize);
    newTableSize = newPrime.prime;

    Node** newTable = (Node**)m_alloc->ArrayAlloc(newTableSize, sizeof(Node*));

    for (unsigned i = 0; i < newTableSize; i++) {
        newTable[i] = NULL;
    }

    // Move all entries over to new table (re-using the Node structures.)

    for (unsigned i = 0; i < m_tableSizeInfo.prime; i++)
    {
        Node* pN = m_table[i];
        while (pN != NULL)
        {
            Node* pNext = pN->m_next;

            unsigned newIndex = magicNumberRem(KeyFuncs::GetHashCode(pN->m_key), newPrime);
            pN->m_next = newTable[newIndex];
            newTable[newIndex] = pN;

            pN = pNext;
        }
    }

    // @todo:
    // We might want to try to delay this cleanup to allow asynchronous readers
    if (m_table != NULL)
        m_alloc->Free(m_table);

    m_table = newTable;
    m_tableSizeInfo = newPrime;
    m_tableMax = (unsigned) (newTableSize * Behavior::s_density_factor_numerator / Behavior::s_density_factor_denominator);
}

// Table of primes and their magic-number-divide constant.
// For more info see the book "Hacker's Delight" chapter 10.9 "Unsigned Division by Divisors >= 1"
// These were selected by looking for primes, each roughly twice as big as the next, having
// 32-bit magic numbers, (because the algorithm for using 33-bit magic numbers is slightly slower).
//

extern const PrimeInfo primeInfo[27];

template <typename Key, typename KeyFuncs, typename Value, typename Behavior>
PrimeInfo SimplerHashTable<Key,KeyFuncs,Value,Behavior>::NextPrime(unsigned number)
{
    for (int i = 0; i < (int) (sizeof(primeInfo) / sizeof(primeInfo[0])); i++) {
        if (primeInfo[i].prime >= number)
            return primeInfo[i];
    }

    // overflow
    Behavior::NoMemory();
}

#endif // _SIMPLERHASHTABLE_INL_
