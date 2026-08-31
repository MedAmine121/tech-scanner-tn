import { BaseDTO } from '../common/base-dto.model';

export interface Context extends BaseDTO {
  email: string;
  fullName: string;
  address: string;
  profilePictureUrl: string;
  balance: number;
  role: number;
  token: string;
  expires: string | Date;
}

