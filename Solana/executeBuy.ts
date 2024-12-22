import axios from 'axios';
import { Wallet } from '@project-serum/anchor';

import { Connection, Keypair, VersionedTransaction, PublicKey, clusterApiUrl, TransactionMessage, AddressLookupTableAccount, SystemProgram } from '@solana/web3.js';

import bs58 from 'bs58';



import dotenv from "dotenv";
dotenv.config();

const baseURL = 'https://bsccentral.velvetdao.xyz/getQuote';

async function main() {
  const connection = new Connection('https://api.mainnet-beta.solana.com', 'confirmed');


  const requestData = {
    slippage: process.env.SLIPPAGE,
    amount: process.env.AMOUNT_IN,
    tokenIn: process.env.TOKEN_IN_ADDRESS,
    tokenOut: process.env.TOKEN_OUT_ADDRESS,
    sender: process.env.SENDER_ADDRESS,
    priorityFee: process.env.PRIORITY_FEE,
  };


  const wallet = new Wallet(Keypair.fromSecretKey(bs58.decode(process.env.PRIVATE_KEY || '')));



  try {
    // Step 1: Get the quote
    const response = await axios.post(baseURL, requestData);
    const quotes = response.data;

    // Check if quotes are returned
    if (!quotes || quotes.length === 0) {
      throw new Error('No quotes returned');
    }


    // Step 2: Extract swapData from the first quote
    const swapData = quotes.swapData;

    const serializedTxBuffer = Buffer.from(swapData, 'base64');
    const transaction = VersionedTransaction.deserialize(serializedTxBuffer);

    transaction.sign([wallet.payer]);
    // Send the transaction
    const latestBlockHash = await connection.getLatestBlockhash();

    // Execute the transactionp.
    const rawTransaction = transaction.serialize()
    const txid = await connection.sendRawTransaction(rawTransaction, {
      skipPreflight: true,
      maxRetries: 2
    });
    await connection.confirmTransaction({
      blockhash: latestBlockHash.blockhash,
      lastValidBlockHeight: latestBlockHash.lastValidBlockHeight,
      signature: txid
    });

    console.log("transaction hash", txid)

    // Log the transaction signature
  } catch (error) {
    console.error('Error executing transaction:', error);
  }
}


main();