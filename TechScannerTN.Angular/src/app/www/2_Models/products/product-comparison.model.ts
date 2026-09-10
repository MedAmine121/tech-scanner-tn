export interface RetailerOfferDto {
  listingId: number;
  retailerId: number;
  retailerCode: string;
  retailerName: string;
  retailerLogoUrl?: string;
  retailerSku: string;
  title: string;
  productUrl: string;
  imageUrl?: string;
  regularPrice: number;
  finalPrice: number;
  discountPercentage: number;
  isInStock: boolean;
  stockStatusText?: string;
  isCheapest: boolean;
  differenceVsCheapest: number;
  differencePercentage: number;
  lastScrapedAt: string;
}

export interface ProductComparisonDto {
  id: number;
  title: string;
  normalizedSku?: string;
  ean?: string;
  brandName?: string;
  categoryName?: string;
  primaryImageUrl?: string;
  description?: string;
  specifications?: string;
  cheapestPrice: number;
  highestPrice: number;
  cheapestRetailerName?: string;
  cheapestRetailerId?: number;
  historicalLowPrice: number;
  historicalHighPrice: number;
  isAtHistoricalLow: boolean;
  offersCount: number;
  maxSavingsAmount: number;
  maxSavingsPercentage: number;
  offers: RetailerOfferDto[];
}

export interface PriceHistoryDto {
  id: number;
  retailerId: number;
  retailerName: string;
  retailerCode: string;
  regularPrice: number;
  finalPrice: number;
  isInStock: boolean;
  recordedAt: string;
}

export interface ProductFilterRequest {
  searchTerm?: string;
  brandId?: number;
  categoryId?: number;
  retailerId?: number;
  minPrice?: number;
  maxPrice?: number;
  onlyInStock?: boolean;
  onlyHistoricalLows?: boolean;
  sortBy?: string;
  page?: number;
  pageSize?: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface RetailerCompetitivenessDto {
  retailerId: number;
  retailerCode: string;
  retailerName: string;
  totalOffersCount: number;
  inStockOffersCount: number;
  cheapestOffersCount: number;
  cheapestMarketSharePercentage: number;
  outOfStockRatePercentage: number;
  averageDiscountPercentage: number;
}

export interface MarketOverviewDto {
  totalCanonicalProducts: number;
  totalListingsTracked: number;
  priceChangesRecordedLast24h: number;
  productsAtAllTimeLow: number;
  retailerStats: RetailerCompetitivenessDto[];
}

